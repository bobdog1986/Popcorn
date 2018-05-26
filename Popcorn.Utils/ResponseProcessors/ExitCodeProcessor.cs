using System;
using System.Collections.Generic;
using Popcorn.Utils.Interfaces;

namespace Popcorn.Utils.ResponseProcessors
{
    internal class ExitCodeProcessor : IResponseProcessor
	{
		StandardResponse IResponseProcessor.ProcessResponse(IEnumerable<string> responseLines, int exitCode, string splitRegEx = null)
		{
			IResponseProcessor response = new StandardResponse();
			response.ProcessResponse(responseLines, exitCode);
			return (StandardResponse)response;
		}
	}
}