using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Web.Infrastructure;
using VirtoCommerce.Platform.Web.Licensing;

namespace VirtoCommerce.Platform.Web.Controllers.Api
{
    [Produces("application/json")]
    [Route("api/platform/licensing")]
    [Authorize(PlatformConstants.Security.Permissions.ModuleManage)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class LicensingController : Controller
    {
        private readonly IHostingEnvironment _hostingEnv;
        private readonly PlatformOptions _platformOptions;
        public LicensingController(IHostingEnvironment hostingEnv, IOptions<PlatformOptions> platformOptions)
        {
            _hostingEnv = hostingEnv;
            _platformOptions = platformOptions.Value;
        }

        [HttpPost]
        [Route("activateByCode")]
        [ProducesResponseType(typeof(License), 200)]
        public async Task<IActionResult> ActivateByCode([FromBody]string activationCode)
        {
            License license = null;

            using (var httpClient = new HttpClient())
            {
                var activationUrl = new Uri(_platformOptions.ActivationUrl + activationCode);
                var httpResponse = await httpClient.GetAsync(activationUrl);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var rawLicense = await httpResponse.Content.ReadAsStringAsync();
                    license = License.Parse(rawLicense, Path.GetFullPath(_platformOptions.LicensePublicKeyPath));
                }
            }

            return Ok(license);
        }

        [HttpPost]
        [Route("activateByFile")]
        [ProducesResponseType(typeof(License), 200)]
        public async Task<IActionResult> ActivateByFile(IFormFile file)
        {
            License license = null;
            var rawLicense = string.Empty;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                rawLicense = await reader.ReadToEndAsync();
            }

            if (!string.IsNullOrEmpty(rawLicense))
            {
                license = License.Parse(rawLicense, Path.GetFullPath(_platformOptions.LicensePublicKeyPath));
            }

            return Ok(license);
        }

        [HttpPost]
        [Route("activateLicense")]
        [ProducesResponseType(typeof(License), 200)]
        public IActionResult ActivateLicense(License license)
        {
            license = License.Parse(license?.RawLicense, Path.GetFullPath(_platformOptions.LicensePublicKeyPath));

            if (license != null)
            {

                var licenseFilePath = Path.GetFullPath(_platformOptions.LicenseFilePath);
                File(licenseFilePath, license.RawLicense);
            }

            return Ok(license);
        }
    }
}
