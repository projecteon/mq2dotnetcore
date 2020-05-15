using System;

namespace MQ2DotNetCore.MQ2Api
{
	public class ChatLineEventArgs : EventArgs
	{
		public ChatLineEventArgs(string chatLine, uint color, uint? filter = null)
		{
			ChatLine = chatLine;
			Color = color;
		}

		public string ChatLine { get; }
		public uint Color { get; }
		public uint? Filter { get; }
	}
}
