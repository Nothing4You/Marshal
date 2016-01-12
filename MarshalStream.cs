using System;
using System.Collections.Generic;
using log4net;

namespace MarshalUtil
{
    public sealed class MarshalStream
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MarshalStream));
        private readonly string _dat;
        private int _index = 2 + 4 * 2;
        private readonly List<object> _storage = new List<object>();
        private object _output;
        private bool _initialized;
        private readonly object _syncLock = new object();

        public MarshalStream(string dat)
        {
            _dat = EvalString.ParseString(dat.Replace(@"\x", @"\x00")).ToHex();
        }

        public object GetValue()
        {
            lock (_syncLock)
            {
                if (_initialized) return _output;

                _output = ProcessSnip();
                _initialized = true;
                return _output;
            }
        }

        private object ProcessSnip()
        {
            // Type identification: http://stackoverflow.com/a/2445940
            object result = null;

            int type = GetBytesBase16(_dat, 1);
            bool shared = (type & ProtocolConstants.SharedFlag) == ProtocolConstants.SharedFlag;
            type &= ~ProtocolConstants.SharedFlag;

            ProtocolType enumType;
            Enum.TryParse(type.ToString(), out enumType);
            switch (enumType)
            {
                case ProtocolType.None:
                    break;
                case ProtocolType.True:
                    result = true;
                    break;
                case ProtocolType.False:
                    result = false;
                    break;

                case ProtocolType.Float0:
                case ProtocolType.Zero:
                    result = 0;
                    break;
                case ProtocolType.One:
                    result = 1;
                    break;
                case ProtocolType.Minusone:
                    result = -1;
                    break;
                case ProtocolType.Int8:
                    result = Convert.ToInt32(GetBytes(_dat, 1).ToBigEndian(), 16);
                    break;
                case ProtocolType.Int16:
                    result = Convert.ToInt32(GetBytes(_dat, 2).ToBigEndian(), 16);
                    break;
                case ProtocolType.Int32:
                    result = Convert.ToInt32(GetBytes(_dat, 4).ToBigEndian(), 16);
                    break;
                case ProtocolType.Int64:
                    result = Convert.ToInt32(GetBytes(_dat, 8).ToBigEndian(), 16);
                    break;
                case ProtocolType.Long:
                    result = Convert.ToInt64(GetBytes(_dat, GetBytesBase16(_dat, 1)).ToBigEndian(), 16);
                    break;
                case ProtocolType.Float:
                    result = Convert.ToDouble(Convert.ToByte(GetBytes(_dat, 8), 16));
                    break;

                case ProtocolType.List0:
                case ProtocolType.Tuple0:
                    result = new List<object>();
                    break;
                case ProtocolType.List1:
                case ProtocolType.Tuple1:
                    List<object> resList1 = new List<object> { ProcessSnip() };
                    result = resList1;
                    break;
                case ProtocolType.Tuple2:
                    List<object> resList2 = new List<object>();
                    for (int i = 0; i < 2; i++)
                    {
                        resList2.Add(ProcessSnip());
                    }
                    result = resList2;
                    break;
                case ProtocolType.List:
                case ProtocolType.Tuple:
                    List<object> resList = new List<object>(GetBytesBase16(_dat, 1));
                    for (int i = 0; i < resList.Capacity; i++)
                    {
                        resList.Add(ProcessSnip());
                    }
                    result = resList;
                    break;

                case ProtocolType.String0:
                case ProtocolType.Unicode0:
                    result = "";
                    break;
                case ProtocolType.String1:
                case ProtocolType.Unicode1:
                    result = GetBytes(_dat, 1).FromHex();
                    break;
                case ProtocolType.String:
                case ProtocolType.Stringl:
                case ProtocolType.Unicode:
                case ProtocolType.Buffer:
                case ProtocolType.Utf8:
                    result = GetBytes(_dat, GetBytesBase16(_dat, 1)).FromHex();
                    break;
                case ProtocolType.Stringr:
                    result = ProtocolConstants.StringTable[GetBytesBase16(_dat, 1)];
                    break;

                case ProtocolType.Mark:
                    result = "(mark)";
                    break;
                case ProtocolType.Reduce:
                    List<object> reduceList = new List<object>();
                    while (true)
                    {
                        object reduceTmpRes = ProcessSnip();
                        if (reduceTmpRes as string == "(mark)")
                        {
                            break;
                        }
                        reduceList.Add(reduceTmpRes);
                    }
                    ProcessSnip(); // remove second mark

                    result = new Dictionary<object, object>
                    {
                        {"(reduce)", reduceList}
                    };
                    break;
                case ProtocolType.Callback:
                    result = new Dictionary<object, object>
                    {
                        {"(callback)", ProcessSnip()}
                    };
                    break;
                case ProtocolType.Global:
                    result = new Dictionary<object, object>
                    {
                        {"(global)", GetBytes(_dat, GetBytesBase16(_dat, 1)).FromHex()}
                    };
                    break;
                case ProtocolType.Newobj:
                    result = new Dictionary<object, object>
                    {
                        {"(newobj)", ProcessSnip()}
                    };
                    break;
                case ProtocolType.Instance:
                    result = new Dictionary<object, object>
                    {
                        {"(instanceName)", ProcessSnip()},
                        {"(instanceData)", ProcessSnip()}
                    };
                    break;

                case ProtocolType.Ref:
                    result = _storage[GetBytesBase16(_dat, 1) - 1];
                    break;

                case ProtocolType.Dict:
                    int dictLength = GetBytesBase16(_dat, 1);
                    Dictionary<object, object> data = new Dictionary<object, object>();
                    for (int i = 0; i < dictLength; i++)
                    {
                        object tmp = ProcessSnip();
                        object tmpind = ProcessSnip();

                        if (!(tmpind is string) && tmp is string)
                        {
                            data[tmp] = tmpind;
                        }
                        else
                        {
                            data[tmpind] = tmp;
                        }
                    }

                    result = data;
                    break;

                case ProtocolType.Stream:
                    int streamTmpIndex = _index;
                    int streamLength = GetBytesBase16(_dat, 1);
                    _index += 2 + (2 * 4);
                    Dictionary<object, object> res = new Dictionary<object, object> { { "(marshal)", new List<object>() } };
                    while (_index < (streamTmpIndex + (streamLength * 2)))
                    {
                        ((List<object>)res["(marshal)"]).Add(ProcessSnip());
                    }
                    result = res;
                    break;

                case ProtocolType.EOF:
                    break;

                // ReSharper disable once RedundantCaseLabel
                case ProtocolType.Checksum:
                // ReSharper disable once RedundantCaseLabel
                case ProtocolType.Compress:
                // ReSharper disable once RedundantCaseLabel
                case ProtocolType.Blue:
                // ReSharper disable once RedundantCaseLabel
                case ProtocolType.Pickler:
                // ReSharper disable once RedundantCaseLabel
                case ProtocolType.Dbrow:
                default:
                    Log.Warn("TYPE " + type + " OFF " + _index);
                    break;
            }

            if (shared)
            {
                _storage.Add(result);
            }
            return result;
        }

        private string GetBytes(string dat, int cnt)
        {
            if (_index >= dat.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(_index), _index, @"Index is higher than dat!");
            }

            string res = dat.Substring(_index, 2 * cnt);
            _index += 2 * cnt;
            return res;
        }

        private int GetBytesBase16(string dat, int cnt)
        {
            return Convert.ToInt32(GetBytes(dat, cnt), 16);
        }
    }
}
