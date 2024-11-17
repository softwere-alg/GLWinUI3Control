using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Win32;

namespace GLWinUI3Control.Utils
{
    /// <summary>
    /// コアの割り当てを管理します。
    /// </summary>
    internal static class CoreManager
    {
        /// <summary>
        /// コアの割り当て数を保持します。
        /// </summary>
        private static Dictionary<nuint, int> _counter = new Dictionary<nuint, int>();

        /// <summary>
        /// プロセスで使用しているコアを定義します。
        /// </summary>
        private static nuint _processAffinityMask;

        /// <summary>
        /// ロックオブジェクトを定義します。
        /// </summary>
        private static object _lock = new object();

        static unsafe CoreManager()
        {
            // プロセスで使用しているコア一覧を取得
            SafeFileHandle processHandle = PInvoke.GetCurrentProcess_SafeHandle();
            PInvoke.GetProcessAffinityMask(processHandle, out _processAffinityMask, out nuint _);

            for (int i = 0; i < sizeof(nuint); i++)
            {
                nuint bitMask = (nuint)1 << i;

                // bitが立っている = 使用できるコア のため追加する
                if ((_processAffinityMask & bitMask) == bitMask)
                {
                    _counter.Add(bitMask, 0);
                }
            }
        }

        /// <summary>
        /// 呼び出し元のスレッドを実行するコアを割り当てます。
        /// </summary>
        /// <param name="affinityMask">自動で割り当てる場合は0</param>
        /// <returns>割り当てたコア</returns>
        public static unsafe nuint AllocateCore(nuint affinityMask)
        {
            lock (_lock)
            {
                // コアの指定がない場合
                if (affinityMask == 0)
                {
                    nuint minBitMask = 0;
                    int minCount = int.MaxValue;

                    // 使用できるコアの中で最も割り当てが少ないコアを調べる
                    for (int i = 0; i < sizeof(nuint); i++)
                    {
                        nuint bitMask = (nuint)1 << i;

                        if ((_processAffinityMask & bitMask) == bitMask)
                        {
                            if (minCount > _counter[bitMask])
                            {
                                minCount = _counter[bitMask];
                                minBitMask = bitMask;
                            }
                        }
                    }

                    if (minBitMask == 0)
                    {
                        Trace.Assert(false, "コア割り当てに失敗しました。");
                        return 0;
                    }
                    else
                    {
                        _counter[minBitMask] += 1;
                        PInvoke.SetThreadAffinityMask(PInvoke.GetCurrentThread(), minBitMask);
                        return minBitMask;
                    }
                }
                // コアの指定がある場合
                else
                {
                    for (int i = 0; i < sizeof(nuint); i++)
                    {
                        nuint bitMask = (nuint)1 << i;

                        // ビットが立っている最下位のコアを割り当てる
                        if ((affinityMask & bitMask) == bitMask)
                        {
                            _counter[bitMask] += 1;
                            PInvoke.SetThreadAffinityMask(PInvoke.GetCurrentThread(), bitMask);
                            return bitMask;
                        }
                    }

                    Trace.Assert(false, "コア割り当てに失敗しました。");
                    return 0;
                }
            }
        }

        /// <summary>
        /// 呼び出し元のスレッドを実行するコアの割り当てを解除します。。
        /// </summary>
        /// <param name="affinityMask">使用中のコア割り当て</param>
        public static void FreeCore(nuint affinityMask)
        {
            lock (_lock)
            {
                if (_counter.ContainsKey(affinityMask))
                {
                    _counter[affinityMask] -= 1;
                    PInvoke.SetThreadAffinityMask(PInvoke.GetCurrentThread(), _processAffinityMask);
                }
                else
                {
                    Trace.WriteLine(false, "コア割り当て解除に失敗しました。");
                }
            }
        }
    }
}
