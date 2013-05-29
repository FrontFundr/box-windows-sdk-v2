﻿using Box.V2.Auth;
using Box.V2.Contracts;
using Box.V2.Models;
using Box.V2.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box.V2.Managers
{
    public class BoxFoldersManager : BoxResourceManager
    {
        public BoxFoldersManager(IBoxConfig config, IBoxService service, IBoxConverter converter, IAuthRepository auth)
            : base(config, service, converter, auth) { }

        /// <summary>
        /// Retrieves the files and/or folders contained in the provided folder id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        public async Task<Folder> GetItemsAsync(string id, int limit, int offset = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid parameters for required fields");

            string accessToken = _auth.Session.AccessToken;

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, id)
                .Authenticate(accessToken)
                .Param("limit", limit.ToString())
                .Param("offset", offset.ToString());

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Used to create a new empty folder. The new folder will be created inside of the specified parent folder
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public async Task<Folder> CreateAsync(BoxFolderRequest folderRequest)
        {
            if (folderRequest == null ||
            string.IsNullOrEmpty(folderRequest.Name) ||
            string.IsNullOrEmpty(folderRequest.Parent.Id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri)
                .Method(RequestMethod.POST);
            request.Payload = _converter.Serialize<BoxFolderRequest>(folderRequest);
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Retrieves the full metadata about a folder, including information about when it was last updated 
        /// as well as the files and folders contained in it. The root folder of a Box account is always 
        /// represented by the id “0″.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> GetInformationAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid parameters for required fields");

            string accessToken = _auth.Session.AccessToken;

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, id)
                .Authenticate(accessToken);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Used to create a copy of a folder in another folder. The original version of the folder will not be altered.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> CopyAsync(BoxFolderRequest folderRequest)
        {
            if (folderRequest == null ||
                string.IsNullOrWhiteSpace(folderRequest.Id) ||
                string.IsNullOrEmpty(folderRequest.Parent.Id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, string.Format(Constants.CopyPathString, folderRequest.Id))
                    .Method(RequestMethod.POST);
            request.Payload = _converter.Serialize(folderRequest);
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Used to delete a folder. A recursive parameter must be included in order to delete folders that have items 
        /// inside of them. An optional If-Match header can be included to ensure that client only deletes the folder 
        /// if it knows about the latest version.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> DeleteAsync(string id, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, id)
                .Method(RequestMethod.DELETE)
                .Param("recursive", recursive.ToString());
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Used to update information about the folder. To move a folder, update the ID of its parent. To enable an 
        /// email address that can be used to upload files to this folder, update the folder_upload_email attribute. 
        /// An optional If-Match header can be included to ensure that client only updates the folder if it knows 
        /// about the latest version.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> UpdateInformationAsync(BoxFolderRequest folderRequest)
        {
            if (folderRequest == null || 
                string.IsNullOrWhiteSpace(folderRequest.Id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, folderRequest.Id)
                    .Method(RequestMethod.PUT);
            request.Payload = _converter.Serialize(folderRequest);
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Used to create a shared link for this particular folder. Please see here for more information on the 
        /// permissions available for shared links. In order to disable a shared link, send this same type of PUT 
        /// request with the value of shared_link set to null, i.e. {"shared_link": null}
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> CreateSharedLinkAsync(string id, BoxSharedLinkRequest sharedLinkRequest)
        {
            CheckPrerequisite(id, sharedLinkRequest.Access);

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, id)
                .Method(RequestMethod.PUT);
            request.Payload = _converter.Serialize(new BoxItemRequest(){ SharedLink = sharedLinkRequest });
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Use this to get a list of all the collaborations on a folder i.e. all of the users that have access to that folder.
        /// </summary>
        /// <returns></returns>
        public async Task<Collection<Collaboration>> GetCollaborationsAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, string.Format(Constants.CollaborationsPathString, id));
            AddAuthentication(request);

            IBoxResponse<Collection<Collaboration>> response = await _service.ToResponseAsync<Collection<Collaboration>>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Retrieves the files and/or folders that have been moved to the trash. Any attribute in the full files 
        /// or folders objects can be passed in with the fields parameter to get specific attributes, and only those 
        /// specific attributes back; otherwise, the mini format is returned for each item by default. Multiple 
        /// attributes can be passed in separated by commas e.g. fields=name,created_at. Paginated results can be 
        /// retrieved using the limit and offset parameters.
        /// </summary>
        /// <returns></returns>
        public async Task<Collection<Item>> GetTrashItemsAsync(string id, int limit, int offset = 0, string fields = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, string.Format(Constants.TrashFolderPathString, id))
                .Param("limit", limit.ToString())
                .Param("offset", offset.ToString())
                .Param("fields", fields);
            AddAuthentication(request);

            IBoxResponse<Collection<Item>> response = await _service.ToResponseAsync<Collection<Item>>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Restores an item that has been moved to the trash. Default behavior is to restore the item to the folder it was in 
        /// before it was moved to the trash. If that parent folder no longer exists or if there is now an item with the same 
        /// name in that parent folder, the new parent folder and/or new name will need to be included in the request.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> RestoreTrashedFolderAsync(BoxFolderRequest folderRequest)
        {
            if (folderRequest == null ||
                string.IsNullOrWhiteSpace(folderRequest.Id) ||
                string.IsNullOrWhiteSpace(folderRequest.Parent.Id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, folderRequest.Id)
                    .Method(RequestMethod.POST);
            request.Payload = _converter.Serialize(folderRequest);
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Permanently deletes an item that is in the trash. The item will no longer exist in Box. This action cannot be undone.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> PurgeTrashedFolderAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, string.Format(Constants.TrashFolderPathString, id))
                .Method(RequestMethod.DELETE);
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }

        /// <summary>
        /// Retrieves a folder that has been moved to the trash.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> GetTrashedFolderAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid parameters for required fields");

            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri, string.Format(Constants.TrashFolderPathString, id));
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }


        /*** DEPRECATED 
        /// <summary>
        /// Retrieves the discussions on a particular folder, if any exist.
        /// </summary>
        /// <returns></returns>
        public async Task<Folder> GetDiscussionsAsync()
        {
            BoxRequest request = new BoxRequest(_config.FoldersEndpointUri);
            AddAuthentication(request);

            IBoxResponse<Folder> response = await _service.ToResponseAsync<Folder>(request);

            return response.ResponseObject;
        }
         ***/

    }
}
