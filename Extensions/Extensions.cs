using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Ookii.Dialogs.Wpf;

namespace OsuModeManager {
    public static class Extensions {
        public static bool TryGetFirst<T>(this IEnumerable<T> Enum, out T Result, bool Enumerate = false) {
            // ReSharper disable PossibleMultipleEnumeration
            if (Enumerate) { Enum = Enum.ToArray(); }
            if (Enum != null && Enum.Any()) {
                Result = Enum.First();
                return Result != null;
            }
            Result = default;
            return false;
            // ReSharper restore PossibleMultipleEnumeration
        }

        public static bool TryGetAt<T>(this T[] Array, int Index, out T Result) {
            if (Array == null || Index < 0 || Array.Length <= Index) {
                Result = default;
                return false;
            }
            Result = Array[Index];
            return Result != null;
        }
        public static bool IsNullOrEmpty(this string String) => string.IsNullOrEmpty(String);

        public static int Clamp(this int Value, int Min = int.MinValue, int Max = int.MaxValue) => Value < Min ? Min : Value > Max ? Max : Value;

        public static Task WaitForExitAsync(this Process Process,
            CancellationToken CancellationToken = default) {
            TaskCompletionSource<object> Tcs = new TaskCompletionSource<object>();
            Process.EnableRaisingEvents = true;
            Process.Exited += (Sender, Args) => Tcs.TrySetResult(null);
            if (CancellationToken != default) {
                CancellationToken.Register(Tcs.SetCanceled);
            }

            return Tcs.Task;
        }

    }
}
