﻿using System;
using System.Linq;
using Castle.DynamicProxy;
using NUnit.Framework;

using DryIoc.Interception;

namespace DryIoc.IssuesTests.Interception
{
    [TestFixture]
    public class InterceptionTests
    {
        [Test]
        public void Test_interface_interception()
        {
            var c = new Container();

            c.Register<ICalculator1, Calculator1>();
            c.InterceptInterface<ICalculator1, CalculatorLogger>();

            var result = string.Empty;
            c.Register<CalculatorLogger>(made:
                Parameters.Of.Type<Action<IInvocation>>(_ => invocation =>
                    result = string.Join("+", invocation.Arguments.Select(x => x.ToString()))));

            var calc = c.Resolve<ICalculator1>();
            calc.Add(1, 2);

            Assert.AreEqual("1+2", result);
        }
    }

    public sealed class CalculatorLogger : IInterceptor
    {
        private readonly Action<IInvocation> _logAction;

        public CalculatorLogger(Action<IInvocation> logAction)
        {
            _logAction = logAction;
        }

        public void Intercept(IInvocation invocation)
        {
            _logAction(invocation);
            invocation.Proceed();
        }
    }

    public interface ICalculator1
    {
        int Add(int first, int second);
    }

    public class Calculator1 : ICalculator1
    {
        public virtual int Add(int first, int second)
        {
            return first + second;
        }
    }
}