// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        static unsafe int Main(string[] args)
        {
            int testResult = Pass;

            if (Avx.IsSupported)
            {
                using (TestTable<float, float, float> floatTable = new TestTable<float, float, float>(new float[8] { 1, -5, 100, 0, 1, -5, 100, 0 }, new float[8] { 22, -1, -50, 0, 22, -1, -50, 0 }, new float[8]))
                using (TestTable<double, double, double> doubleTable = new TestTable<double, double, double>(new double[4] { 1, -5, 100, 0 }, new double[4] { 22, -1, -50, 0 }, new double[4]))
                {
                    var vf1 = Unsafe.Read<Vector256<float>>(floatTable.inArray1Ptr);
                    var vf2 = Unsafe.Read<Vector256<float>>(floatTable.inArray2Ptr);
                    var vf3 = Avx.Multiply(vf1, vf2);
                    Unsafe.Write(floatTable.outArrayPtr, vf3);

                    var vd1 = Unsafe.Read<Vector256<double>>(doubleTable.inArray1Ptr);
                    var vd2 = Unsafe.Read<Vector256<double>>(doubleTable.inArray2Ptr);
                    var vd3 = Avx.Multiply(vd1, vd2);
                    Unsafe.Write(doubleTable.outArrayPtr, vd3);

                    if (!floatTable.CheckResult((x, y, z) => x * y == z))
                    {
                        Console.WriteLine("AVX Multiply failed on float:");
                        foreach (var item in floatTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }

                    if (!doubleTable.CheckResult((x, y, z) => x * y == z))
                    {
                        Console.WriteLine("AVX Multiply failed on double:");
                        foreach (var item in doubleTable.outArray)
                        {
                            Console.Write(item + ", ");
                        }
                        Console.WriteLine();
                        testResult = Fail;
                    }
                }
            }
            return testResult;
        }

        public unsafe struct TestTable<T1, T2, T3> : IDisposable where T1 : struct where T2 : struct where T3 : struct
        {
            public T1[] inArray1;
            public T2[] inArray2;
            public T3[] outArray;

            public void* inArray1Ptr => inHandle1.AddrOfPinnedObject().ToPointer();
            public void* inArray2Ptr => inHandle2.AddrOfPinnedObject().ToPointer();
            public void* outArrayPtr => outHandle.AddrOfPinnedObject().ToPointer();

            GCHandle inHandle1;
            GCHandle inHandle2;
            GCHandle outHandle;
            public TestTable(T1[] a, T2[] b, T3[] c)
            {
                this.inArray1 = a;
                this.inArray2 = b;
                this.outArray = c;

                inHandle1 = GCHandle.Alloc(inArray1, GCHandleType.Pinned);
                inHandle2 = GCHandle.Alloc(inArray2, GCHandleType.Pinned);
                outHandle = GCHandle.Alloc(outArray, GCHandleType.Pinned);
            }
            public bool CheckResult(Func<T1, T2, T3, bool> check)
            {
                for (int i = 0; i < inArray1.Length; i++)
                {
                    if (!check(inArray1[i], inArray2[i], outArray[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public void Dispose()
            {
                inHandle1.Free();
                inHandle2.Free();
                outHandle.Free();
            }
        }

    }
}