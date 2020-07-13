using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace MQ2DotNetCore.Logging
{
	/// <summary>
	/// An <see cref="ILoggerProvider" /> implementation that writes logs to the MQ2 chat window
	/// </summary>
	[ProviderAlias("MQ2")]
	public class MQ2LoggerProvider : ILoggerProvider, ISupportExternalScope
	{
		private bool _includeScopes;
		private readonly IDisposable _optionsChangeToken;
		private IExternalScopeProvider? _scopeProvider;

		/// <summary>
		/// Creates an instance of the <see cref="MQ2LoggerProvider" /> 
		/// </summary>
		/// <param name="options">The options object controlling the logger</param>
		public MQ2LoggerProvider(IOptionsMonitor<MQ2LoggerOptions> options)
		{
			_ = options ?? throw new ArgumentNullException(nameof(options));

			// NOTE: Only IncludeScopes is monitored
			_optionsChangeToken = options.OnChange(UpdateOptions);
			UpdateOptions(options.CurrentValue);
		}

		public IExternalScopeProvider? ScopeProvider => _includeScopes
			? _scopeProvider
			: null;

		public ILogger CreateLogger(string categoryName)
		{
			return new MQ2Logger(categoryName, this);
		}

		public void Dispose()
		{
			_optionsChangeToken?.Dispose();
		}

		public void SetScopeProvider(IExternalScopeProvider scopeProvider)
		{
			_scopeProvider = scopeProvider;
		}

		private void UpdateOptions(MQ2LoggerOptions options)
		{
			_includeScopes = options.IncludeScopes;
		}
	}
}
