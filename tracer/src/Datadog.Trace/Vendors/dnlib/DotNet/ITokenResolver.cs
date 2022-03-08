//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendors tool.
//------------------------------------------------------------------------------
// dnlib: See LICENSE.txt for more info

namespace Datadog.Trace.Vendors.dnlib.DotNet {
	/// <summary>
	/// Resolves tokens
	/// </summary>
	internal interface ITokenResolver {
		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="token">The metadata token</param>
		/// <param name="gpContext">Generic parameter context</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		IMDTokenProvider ResolveToken(uint token, GenericParamContext gpContext);
	}

	internal static partial class Extensions {
		/// <summary>
		/// Resolves a token
		/// </summary>
		/// <param name="self">This</param>
		/// <param name="token">The metadata token</param>
		/// <returns>A <see cref="IMDTokenProvider"/> or <c>null</c> if <paramref name="token"/> is invalid</returns>
		public static IMDTokenProvider ResolveToken(this ITokenResolver self, uint token) => self.ResolveToken(token, new GenericParamContext());
	}
}
