namespace MQ2DotNetCore.Logging
{
	public class MQ2LoggerOptions
	{
		/// <summary>
		/// Gets or sets a value indicating whether scopes should be included in the message.
		/// Defaults to <c>false</c>.
		/// </summary>
		public bool IncludeScopes { get; set; } = false;
	}
}
