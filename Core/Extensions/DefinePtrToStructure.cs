using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ISOLib.Core
{
    public static class DefinePtrToStructure
    {
        public static T PtrToStructureT<T>(this IntPtr m_pMsg) where T : struct
        {
            try
            {
                T CamObj = (T)Marshal.PtrToStructure(m_pMsg, typeof(T));
                return CamObj;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return default(T);
            }
        }
        public static T[] PtrToStructureArrT<T>(this IntPtr m_pMsg, uint num, T value)
        {
            /*Console.WriteLine(m_pMsg);*/
            try
            {
                T[] array = new T[num];
                int onesize = 0;
                for (int i = 0; i < num; i++)
                {

                    T CamObj = (T)Marshal.PtrToStructure(m_pMsg + onesize, typeof(T));

                    onesize += Marshal.SizeOf(value);
                    array[i] = CamObj;

                }

                return array;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return default(T[]);
            }
        }
        public static IntPtr ToIntPtr<T>(this T value) where T : struct
        {
            int len = Marshal.SizeOf(value);
            IntPtr IntPtrValue = Marshal.AllocHGlobal(Marshal.SizeOf(value));
            Marshal.StructureToPtr(value, IntPtrValue, false);
            return IntPtrValue;
        }
        public static IntPtr ToIntPtrByArr<T>(this T[] value) where T : struct
        {

            int size = 0;

            foreach (T one in value)
            {
                size = size + Marshal.SizeOf(one);
            }
            IntPtr IntPtrValue = Marshal.AllocHGlobal(size);
            size = 0;
            foreach (T oneSconfig in value)
            {
                Marshal.StructureToPtr(oneSconfig, (IntPtr)(IntPtrValue + size), false);
                size = size + Marshal.SizeOf(oneSconfig);
            }
            return IntPtrValue;
        }
        public static IntPtr ToIntPtrByArr<T>(this T[] value, IntPtr IntPtr) where T : struct
        {

            int size = 0;

            foreach (T one in value)
            {
                size = size + Marshal.SizeOf(one);
            }
            IntPtr IntPtrValue = IntPtr;
            size = 0;
            foreach (T oneSconfig in value)
            {
                Marshal.StructureToPtr(oneSconfig, (IntPtr)(IntPtrValue + size), false);
                size = size + Marshal.SizeOf(oneSconfig);
            }
            return IntPtrValue;
        }

        public static IntPtr ToIntPtrByArr<T>(this List<T> value, IntPtr IntPtr) where T : struct
        {

            int size = 0;

            foreach (T one in value)
            {
                size = size + Marshal.SizeOf(one);
            }
            IntPtr IntPtrValue = IntPtr;
            size = 0;
            foreach (T oneSconfig in value)
            {
                Marshal.StructureToPtr(oneSconfig, (IntPtr)(IntPtrValue + size), false);
                size = size + Marshal.SizeOf(oneSconfig);
            }
            return IntPtrValue;
        }

        public static IntPtr ToIntPtrByT<T>(this T value, IntPtr IntPtr) where T : struct
        {

            int size = 0;


            {
                size = size + Marshal.SizeOf(value);
            }
            IntPtr IntPtrValue = IntPtr;
            size = 0;
            T oneSconfig = value;
            // foreach (T oneSconfig in value)
            {
                Marshal.StructureToPtr(oneSconfig, (IntPtr)(IntPtrValue + size), false);
                size = size + Marshal.SizeOf(oneSconfig);
            }
            return IntPtrValue;
        }
    }
}
