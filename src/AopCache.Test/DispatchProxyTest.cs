//using System;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Text;
//using NUnit.Framework;

//namespace AopCache.Test
//{
//    public class DispatchProxyTest
//    {
//        [SetUp]
//        public void Setup()
//        {
//        }

//        [Test]
//        public void Test1()
//        {
//            //使用DispatchProxy类的静态方法Create生成代理类，其中Create是个泛型方法，泛型有两个值，第一个值必须是接口，第二个值必须是DispatchProxy的子类
//            IMessage messageDispatchProxy = DispatchProxy.Create<IMessage, LogDispatchProxy>();
//            //创建一个实现了IMessage接口的类的实例，并赋值给代理类的TargetClass属性
//            ((LogDispatchProxy)messageDispatchProxy).TargetClass = new EmailMessage();
//            messageDispatchProxy.Send("早上好");
//            Console.WriteLine("=======================================");
//            messageDispatchProxy.Receive("中午好");
//        }
//    }

//    public interface IMessage
//    {
//        void Send(string content);
//        void Receive(string content);
//    }

//    public class EmailMessage : IMessage
//    {
//        [aaa]
//        public void Send(string content)
//        {
//            Console.WriteLine("Send Email:" + content);
//        }
//        public void Receive(string content)
//        {
//            Console.WriteLine("Receive Email:" + content);
//        }
//    }

//    public class LogDispatchProxy : DispatchProxy
//    {
//        public object TargetClass { get; set; }
//        protected override object Invoke(MethodInfo targetMethod, object[] args)
//        {
//            Write("方法执行前");
//            var result = targetMethod.Invoke(TargetClass, args);
//            Write("方法执行后");
//            return result;
//        }

//        private void Write(string content)
//        {
//            Console.ForegroundColor = ConsoleColor.Red;
//            Console.WriteLine(content);
//            Console.ResetColor();
//        }
//    }

//    public class aaa : Attribute
//    {

//    }
//}