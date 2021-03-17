using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TechTalk.SpecFlow.Bindings.Reflection;
using TechTalk.SpecFlow.Compatibility;
using TechTalk.SpecFlow.ErrorHandling;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace TechTalk.SpecFlow.Bindings
{
    public class BindingInvoker : IBindingInvoker
    {
        private static readonly Stopwatch watch = Stopwatch.StartNew();

        protected readonly Configuration.SpecFlowConfiguration specFlowConfiguration;
        protected readonly IErrorProvider errorProvider;
        protected readonly ISynchronousBindingDelegateInvoker synchronousBindingDelegateInvoker;

        public BindingInvoker(Configuration.SpecFlowConfiguration specFlowConfiguration, IErrorProvider errorProvider, ISynchronousBindingDelegateInvoker synchronousBindingDelegateInvoker)
        {
            this.specFlowConfiguration = specFlowConfiguration;
            this.errorProvider = errorProvider;
            this.synchronousBindingDelegateInvoker = synchronousBindingDelegateInvoker;
        }

        public virtual object InvokeBinding(IBinding binding, IContextManager contextManager, object[] arguments, ITestTracer testTracer, out TimeSpan duration)
        {
            EnsureReflectionInfo(binding, out var methodInfo, out var bindingAction);
            var startTime = watch.Elapsed;
            try
            {
                object result;
                using (CreateCultureInfoScope(contextManager))
                {
                    if (methodInfo.IsStatic)
                    {
                        result = synchronousBindingDelegateInvoker.InvokeDelegateSynchronously(bindingAction, arguments);
                    }
                    else
                    {
                        object[] invokeArgs;
                        if (arguments is null)
                        {
                            invokeArgs = new object[1];
                        }
                        else
                        {
                            invokeArgs = new object[arguments.Length + 1];
                            arguments.CopyTo(invokeArgs, 1);
                        }

                        invokeArgs[0] = contextManager.ScenarioContext.GetBindingInstance(methodInfo.ReflectedType);
                        result = synchronousBindingDelegateInvoker.InvokeDelegateSynchronously(bindingAction, invokeArgs);
                    }
                }

                duration = watch.Elapsed - startTime;

                if (specFlowConfiguration.TraceTimings && duration >= specFlowConfiguration.MinTracedDuration)
                {
                    testTracer.TraceDuration(duration, binding.Method, arguments);
                }

                return result;
            }
            catch (ArgumentException ex)
            {
                duration = watch.Elapsed - startTime;
                throw errorProvider.GetCallError(binding.Method, ex);
            }
            catch (TargetInvocationException invEx)
            {
                duration = watch.Elapsed - startTime;
                var ex = invEx.InnerException;
                ex = ex.PreserveStackTrace(errorProvider.GetMethodText(binding.Method));
                throw ex;
            }
            catch (AggregateException aggregateEx)
            {
                duration = watch.Elapsed - startTime;
                var ex = aggregateEx.InnerExceptions.First();
                ex = ex.PreserveStackTrace(errorProvider.GetMethodText(binding.Method));
                throw ex;
            }
            catch (Exception)
            {
                duration = watch.Elapsed - startTime;
                throw;
            }
        }

        protected virtual CultureInfoScope CreateCultureInfoScope(IContextManager contextManager)
        {
            return new CultureInfoScope(contextManager.FeatureContext);
        }

        protected void EnsureReflectionInfo(IBinding binding, out MethodInfo methodInfo, out Delegate bindingAction)
        {
            var methodBinding = binding as MethodBinding;
            if (methodBinding is null)
                throw new SpecFlowException("The binding method cannot be used for reflection: " + binding);

            methodInfo = methodBinding.Method.AssertMethodInfo();
            bindingAction = methodBinding.cachedBindingDelegate;
            if (bindingAction is null)
            {
                bindingAction = methodBinding.cachedBindingDelegate = CreateMethodDelegate(methodInfo);
            }
        }

        protected virtual Delegate CreateMethodDelegate(MethodInfo method)
        {
            if (method.IsStatic)
            {
                return CreateStaticMethodDelegate(method);
            }

            return CreateInstanceMethodDelegate(method);
        }

        private Delegate CreateStaticMethodDelegate(MethodInfo method)
        {
            var parameters = method.GetParameters();

            Type[] parameterTypes;
            if (parameters.Length == 0)
            {
                parameterTypes = Type.EmptyTypes;
            }
            else
            {
                parameterTypes = new Type[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    parameterTypes[i] = parameters[i].ParameterType;
                }
            }

            return method.CreateDelegate(GetDelegateType(parameterTypes, method.ReturnType), null);
        }

        private Delegate CreateInstanceMethodDelegate(MethodInfo method)
        {
            var instanceType = method.ReflectedType;
            var parameters = method.GetParameters();

            var parameterTypes = new Type[parameters.Length + 1];
            parameterTypes[0] = instanceType;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterInfo = parameters[i];
                parameterTypes[i + 1] = parameterInfo.ParameterType;
            }

            return method.CreateDelegate(GetDelegateType(parameterTypes, method.ReturnType), null);
        }

        #region extended action types
        public delegate void ExtendedAction<T1, T2, T3, T4, T5>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19);
        public delegate void ExtendedAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, T20 arg20);

        protected Type GetActionType(Type[] typeArgs)
        {
            Type openType;
            switch (typeArgs.Length)
            {
                case 00: return typeof(Action);
                case 01: openType = typeof(Action<>); break;
                case 02: openType = typeof(Action<,>); break;
                case 03: openType = typeof(Action<,,>); break;
                case 04: openType = typeof(Action<,,,>); break;
                case 05: openType = typeof(ExtendedAction<,,,,>); break;
                case 06: openType = typeof(ExtendedAction<,,,,,>); break;
                case 07: openType = typeof(ExtendedAction<,,,,,,>); break;
                case 08: openType = typeof(ExtendedAction<,,,,,,,>); break;
                case 09: openType = typeof(ExtendedAction<,,,,,,,,>); break;
                case 10: openType = typeof(ExtendedAction<,,,,,,,,,>); break;
                case 11: openType = typeof(ExtendedAction<,,,,,,,,,,>); break;
                case 12: openType = typeof(ExtendedAction<,,,,,,,,,,,>); break;
                case 13: openType = typeof(ExtendedAction<,,,,,,,,,,,,>); break;
                case 14: openType = typeof(ExtendedAction<,,,,,,,,,,,,,>); break;
                case 15: openType = typeof(ExtendedAction<,,,,,,,,,,,,,,>); break;
                case 16: openType = typeof(ExtendedAction<,,,,,,,,,,,,,,,>); break;
                case 17: openType = typeof(ExtendedAction<,,,,,,,,,,,,,,,,>); break;
                case 18: openType = typeof(ExtendedAction<,,,,,,,,,,,,,,,,,>); break;
                case 19: openType = typeof(ExtendedAction<,,,,,,,,,,,,,,,,,,>); break;
                case 20: openType = typeof(ExtendedAction<,,,,,,,,,,,,,,,,,,,>); break;
                default: throw errorProvider.GetTooManyBindingParamError(20);
            }

            return openType.MakeGenericType(typeArgs);
        }
        #endregion

        #region extended func types
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19);
        public delegate TResult ExtendedFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, T20 arg20);

        protected Type GetFuncType(Type[] typeArgs, Type resultType)
        {
            Type openType;
            switch (typeArgs.Length)
            {
                case 00: openType = typeof(Func<>); break;
                case 01: openType = typeof(Func<,>); break;
                case 02: openType = typeof(Func<,,>); break;
                case 03: openType = typeof(Func<,,,>); break;
                case 04: openType = typeof(Func<,,,,>); break;
                case 05: openType = typeof(ExtendedFunc<,,,,,>); break;
                case 06: openType = typeof(ExtendedFunc<,,,,,,>); break;
                case 07: openType = typeof(ExtendedFunc<,,,,,,,>); break;
                case 08: openType = typeof(ExtendedFunc<,,,,,,,,>); break;
                case 09: openType = typeof(ExtendedFunc<,,,,,,,,,>); break;
                case 10: openType = typeof(ExtendedFunc<,,,,,,,,,,>); break;
                case 11: openType = typeof(ExtendedFunc<,,,,,,,,,,,>); break;
                case 12: openType = typeof(ExtendedFunc<,,,,,,,,,,,,>); break;
                case 13: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,>); break;
                case 14: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,,>); break;
                case 15: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,,,>); break;
                case 16: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,,,,>); break;
                case 17: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,,,,,>); break;
                case 18: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,,,,,,>); break;
                case 19: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,,,,,,,>); break;
                case 20: openType = typeof(ExtendedFunc<,,,,,,,,,,,,,,,,,,,,>); break;
                default: throw errorProvider.GetTooManyBindingParamError(20);
            }

            Array.Resize(ref typeArgs, typeArgs.Length + 1);
            typeArgs[typeArgs.Length - 1] = resultType;
            return openType.MakeGenericType(typeArgs);
        }

        protected Type GetDelegateType(Type[] typeArgs, Type resultType)
        {
            if (resultType == typeof(void))
                return GetActionType(typeArgs);

            return GetFuncType(typeArgs, resultType);
        }

        #endregion
    }
}