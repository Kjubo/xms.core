﻿using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime;
using System.Runtime.Serialization;
using System.Reflection;

namespace XMS.Core.Messaging
{
	/// <summary>
	/// 表示在消息总线调用、处理过程中由消息总线引发的异常。
	/// </summary>
	[Serializable, ComVisible(true)]
	public class MessageBusException : Exception
	{
		/// <summary>
		/// 初始化 <see cref="MessageBusException"/> 类的一个新实例。
		/// </summary>
		/// <remarks>
		/// 此构造函数将新实例的 Message 属性初始化为系统提供的消息，该消息对错误进行描述，例如“业务操作的过程中发生错误。”。
		/// </remarks>
		public MessageBusException()
			: base("消息总线调用或处理的过程中发生错误。")
		{
		}

		/// <summary>
		/// 使用指定的错误消息和导致此异常的参数的名称来初始化 <see cref="MessageBusException"/> 类的实例。
		/// </summary>
		/// <param name="message">描述错误的消息。</param>
		/// <remarks>
		/// 此构造函数使用 message 参数的值初始化新实例的 Message 属性。 message 参数的内容应为人所理解。
		/// </remarks>
		public MessageBusException(string message)
			: base(String.IsNullOrEmpty(message) ? "消息总线调用或处理的过程中发生错误。" : message)
		{
		}

		/// <summary>
		/// 使用指定的错误消息和引发此异常的异常初始化 <see cref="MessageBusException"/> 类的新实例。
		/// </summary>
		/// <param name="message">说明发生此异常的原因的错误消息。</param>
		/// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则是一个空引用。</param>
		public MessageBusException(string message, Exception innerException)
			: base(String.IsNullOrEmpty(message) ? "消息总线调用或处理的过程中发生错误。" : message, innerException)
		{
		}

				/// <summary>
		/// 用序列化数据初始化 <see cref="ArgumentNullOrWhiteSpaceException"/> 类的新实例。
		/// </summary>
		/// <param name="info">保存序列化对象数据的对象。</param>
		/// <param name="context">对象，描述序列化数据的源或目标。</param>
		/// <remarks>在反序列化过程中调用该构造函数来重建通过流传输的异常对象。</remarks>
		[SecurityCritical, TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
		protected MessageBusException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
