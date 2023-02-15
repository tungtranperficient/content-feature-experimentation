using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.AddOns.Helpers;
using EPiServer.Core;
using EPiServer.Forms.Core;
using EPiServer.Forms.Core.Internal;
using EPiServer.Forms.Core.Models;
using EPiServer.Forms.Core.Models.Internal;
using EPiServer.Forms.Implementation.Elements;
using EPiServer.Framework.Serialization.Json.Internal;
using EPiServer.Web.Routing;
using Microsoft.AspNetCore.Mvc;

namespace EPiServer.Marketing.Testing.Web.Controllers
{

    [ApiController]
    [Route("api/episerver/abtesting/form")]
    public class FormController : ControllerBase
    {

        private readonly IContentLoader _contentLoader;
        private readonly IContentVersionRepository _contentVersionRepository;
        private readonly UrlResolver _urlResolver;
        private readonly FormRepository _formRepository;
        private readonly FormBusinessService _formBusinessService;

        public FormController(IContentLoader contentLoader, UrlResolver urlResolver, FormRepository formRepository, FormBusinessService formBusinessService, IContentVersionRepository contentVersionRepository)
        {
            _contentLoader = contentLoader;
            _urlResolver = urlResolver;
            _formRepository = formRepository;
            _formBusinessService = formBusinessService;
            _contentVersionRepository = contentVersionRepository;
        }


        [Route("getColumns")]
        [HttpGet]
        [AppSettingsAuthorize(Roles = "CmsAdmins, CmsEditors")]
        public IActionResult GetColumns(int formId, string language)
        {
            var formContentRef = new ContentReference(formId);
            var formIden = new FormIdentity(formContentRef.GetContentGuid(), language);
            IEnumerable<object> columns = null;
            List<FriendlyNameInfo> list = this._formRepository.GetDataFriendlyNameInfos(formIden).ToList<FriendlyNameInfo>();
            IEnumerable<string> searchableElementNames = this._formBusinessService.GetSearchableElementNames(formIden, list.Select<FriendlyNameInfo, string>((Func<FriendlyNameInfo, string>)(m => m.ElementId)));

            //TODO: check if a/b test is enable
            if (false)
            {
                columns = list.Select(m => new
                {
                    Id = m.ElementId,
                    Field = StringUtility.ToCamelCase(m.ElementId),
                    Label = m.FriendlyName,
                    formatType = m.FormatType.ToString(),
                    isSearchable = searchableElementNames.Contains<string>(m.ElementId)
                });
            }
            // get all columns from all versions
            else
            {
                IEnumerable<FriendlyNameInfo> friendlyNameInfos = Enumerable.Empty<FriendlyNameInfo>();
                List<Type> excludedElementTypes = new List<Type>()
                {
                    typeof (IExcludeInSubmission)
                };

                var versions = _contentVersionRepository.List(formContentRef).Where(v => v.LanguageBranch.Equals(language));
                foreach (var version in versions)
                {
                    var formContainerBlock = _contentLoader.Get<FormContainerBlock>(version.ContentLink);

                    if (formContainerBlock != null)
                    {
                        var versionedFriendlyNameInfos = this._formRepository.GetFriendlyNameInfos(formContainerBlock, excludedElementTypes.ToArray());
                        friendlyNameInfos = friendlyNameInfos.UnionBy(versionedFriendlyNameInfos, i => i.ElementId);
                    }
                }

                // merge with SystemNameInfos
                friendlyNameInfos = friendlyNameInfos.Union<FriendlyNameInfo>(_formRepository.GetSystemFriendlyNameInfos());

                columns = friendlyNameInfos.Select(m => new
                {
                    Id = m.ElementId,
                    Field = StringUtility.ToCamelCase(m.ElementId),
                    Label = m.FriendlyName,
                    formatType = m.FormatType.ToString(),
                    isSearchable = searchableElementNames.Contains<string>(m.ElementId)
                });
            }

            return Ok(columns);
        }
    }
}
