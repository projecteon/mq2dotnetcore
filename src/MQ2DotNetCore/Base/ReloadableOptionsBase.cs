using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;

namespace MQ2DotNetCore.Base
{
	public abstract class ReloadableOptionsBase
	{
		protected readonly IConfiguration _configuration;

		protected ReloadableOptionsBase(IConfiguration configuration)
		{
			if (configuration == null)
			{
				throw new ArgumentNullException(nameof(configuration));
			}

			_configuration = configuration;
			configuration.Bind(this);

			ChangeToken.OnChange<ReloadableOptionsBase>(
				() => configuration.GetReloadToken(),
				ReloadOptions,
				this
			);
		}

		protected static void ReloadOptions(ReloadableOptionsBase state)
		{
			state?._configuration?.Bind(state);
		}
	}
}
