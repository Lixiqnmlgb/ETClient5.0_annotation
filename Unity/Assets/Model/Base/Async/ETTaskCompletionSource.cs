using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace ETModel
{
    public class ETTaskCompletionSource: IAwaiter
    {
        // State(= AwaiterStatus)
        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int state;
        private ExceptionDispatchInfo exception;
        private Action continuation; // action or list

        AwaiterStatus IAwaiter.Status => (AwaiterStatus) state;

        bool IAwaiter.IsCompleted => state != Pending;

        public ETTask Task => new ETTask(this);

        void IAwaiter.GetResult()
        {
            switch (this.state)
            {
                case Succeeded://完成
                    return;
                case Faulted://发现错误 进行抛出
                    this.exception?.Throw();
                    this.exception = null;
                    return;
                case Canceled://取消了 抛出取消的异常
                {
                    this.exception?.Throw(); // guranteed operation canceled exception.
                    this.exception = null;
                    throw new OperationCanceledException();
                }
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }
  
        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            //缓存回调
            this.continuation = action;
            //如果状态不是等待中
            if (state != Pending)
            {
                //尝试继续调用以上赋值的action
                TryInvokeContinuation();
            }
        }

        private void TryInvokeContinuation()
        {
            this.continuation?.Invoke();
            //调用后即刻释放
            this.continuation = null;
        }

        //设置结果
        public void SetResult()
        {
            if (this.TrySetResult())
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        //设置异常
        public void SetException(Exception e)
        {
            if (this.TrySetException(e))
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        //尝试设置结果
        public bool TrySetResult()
        {
            //如果不是等待状态
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Succeeded;
            //尝试继续调用上下文的下半部分 
            this.TryInvokeContinuation();
            return true;

        }
        //尝试设置异常
        public bool TrySetException(Exception e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Faulted;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        //尝试设置取消
        public bool TrySetCanceled()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.TryInvokeContinuation();
            return true;

        }
        //尝试设置取消 带异常信息
        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;
            //捕捉的异常
            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }
        
        //当完成的时候
        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion) this).UnsafeOnCompleted(action);
        }
    }

    public class ETTaskCompletionSource<T>: IAwaiter<T>
    {
        // State(= AwaiterStatus)
        private const int Pending = 0;
        private const int Succeeded = 1;
        private const int Faulted = 2;
        private const int Canceled = 3;

        private int state;
        private T value;
        private ExceptionDispatchInfo exception;
        private Action continuation; // action or list

        bool IAwaiter.IsCompleted => state != Pending;

        public ETTask<T> Task => new ETTask<T>(this);

        AwaiterStatus IAwaiter.Status => (AwaiterStatus) state;

        //获取结果
        T IAwaiter<T>.GetResult()
        {
            switch (this.state)
            {
               
                case Succeeded: //完成
                    return this.value;
                case Faulted:
                    this.exception?.Throw();//错误
                    this.exception = null;
                    return default;
                case Canceled://取消
                {
                    this.exception?.Throw(); // guranteed operation canceled exception.
                    this.exception = null;
                    throw new OperationCanceledException();
                }
                default:
                    throw new NotSupportedException("ETTask does not allow call GetResult directly when task not completed. Please use 'await'.");
            }
        }

        void ICriticalNotifyCompletion.UnsafeOnCompleted(Action action)
        {
            this.continuation = action;
            //不是等待状态 就继续执行
            if (state != Pending)
            {
                TryInvokeContinuation();
            }
        }

        private void TryInvokeContinuation()
        {
            this.continuation?.Invoke();
            this.continuation = null;
        }

        public void SetResult(T result)
        {
            if (this.TrySetResult(result))
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public void SetException(Exception e)
        {
            if (this.TrySetException(e))
            {
                return;
            }

            throw new InvalidOperationException("TaskT_TransitionToFinal_AlreadyCompleted");
        }

        public bool TrySetResult(T result)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Succeeded;

            this.value = result;
            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetException(Exception e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Faulted;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled()
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.TryInvokeContinuation();
            return true;

        }

        public bool TrySetCanceled(OperationCanceledException e)
        {
            if (this.state != Pending)
            {
                return false;
            }

            this.state = Canceled;

            this.exception = ExceptionDispatchInfo.Capture(e);
            this.TryInvokeContinuation();
            return true;

        }

        void IAwaiter.GetResult()
        {
            ((IAwaiter<T>) this).GetResult();
        }

        void INotifyCompletion.OnCompleted(Action action)
        {
            ((ICriticalNotifyCompletion) this).UnsafeOnCompleted(action);
        }
    }
}