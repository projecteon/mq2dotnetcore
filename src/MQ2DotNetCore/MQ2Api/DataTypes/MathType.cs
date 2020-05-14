﻿using MQ2DotNetCore.Interop;
using System;

namespace MQ2DotNetCore.MQ2Api.DataTypes
{
	/// <summary>
	/// Contains various mathematical functions. Not implemented for .NET
	/// </summary>
	[Obsolete("Use System.Math")]
	[MQ2Type("math")]
	public class MathType : MQ2DataType
	{
		internal MathType(MQ2TypeFactory mq2TypeFactory, MQ2TypeVar typeVar)
			: base(mq2TypeFactory, typeVar)
		{
		}
	}
}
