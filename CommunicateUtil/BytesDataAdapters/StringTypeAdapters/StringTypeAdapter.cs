using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunicateUtil.BytesDataAdapters.StringTypeAdapters
{
    public class StringTypeAdapter
    {
        public static byte[] GetBytes(string value,int arrayLengh)
        {
            var strbytes = System.Text.Encoding.UTF8.GetBytes(value);
            strbytes = strbytes.Length < arrayLengh ? strbytes.Concat(Enumerable.Repeat((byte)0x00, arrayLengh - strbytes.Length)).ToArray() : strbytes;
            return strbytes;
        }

        public static string GetString(byte[] bytes, int arrayLengh)
        {
            //删除结尾0x00的数据
            int dataLengh = bytes.Take(arrayLengh).TakeWhile(a => a != 0x00).Count();
            string value = Encoding.UTF8.GetString(bytes.ToArray(), 0, dataLengh);
            return value;
        }
    }
}
