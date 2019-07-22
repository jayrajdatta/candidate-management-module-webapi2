using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace CMMService.Controllers
{
    [EnableCors(origins: "http://localhost:4200", headers: "*", methods: "*")]
    public class CandidatesController : ApiController
    {
        private const string Cache_Key_Candidates = "candidateprofiledata";

        [HttpGet]
        [ActionName("DefaultAction")]
        public HttpResponseMessage GetCandidates()
        {
            using (CandidateProfileDBContext candidateProfileDBContext = new CandidateProfileDBContext())
            {
                //var candidates = candidateProfileDBContext.Candidates.ToList();
                //return candidates;

                ObjectCache cache = MemoryCache.Default;

                //caching enabled for fast data access
                if (cache.Contains(Cache_Key_Candidates))
                    return Request.CreateResponse(HttpStatusCode.OK, cache.Get(Cache_Key_Candidates));
                else
                {
                    var candidates = candidateProfileDBContext.Candidates.Select(c => new { c.CandidateId, c.FullName, c.DOB, c.Domain, c.ProfileDocument.DocumentName }).OrderBy(c => c.FullName).ToList();

                    // Store data in the cache    
                    CacheItemPolicy cacheItemPolicy = new CacheItemPolicy();
                    cacheItemPolicy.AbsoluteExpiration = DateTime.Now.AddHours(2.0);
                    cache.Add(Cache_Key_Candidates, candidates, cacheItemPolicy);

                    return Request.CreateResponse(HttpStatusCode.OK, candidates);
                }
            }
        }

        [HttpGet]
        public HttpResponseMessage GetCandidate(int id)
        {
            using (CandidateProfileDBContext candidateProfileDBContext = new CandidateProfileDBContext())
            {
                var candidate = candidateProfileDBContext.Candidates.Select(c => new { c.CandidateId, c.FullName, c.DOB, c.Domain, c.ProfileDocument.DocumentName, c.ProfileDocument.DocumentData }).Where(can => can.CandidateId == id).FirstOrDefault();

                if(candidate!= null)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, candidate);
                }
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddCandidate(Candidate candidate)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            try
            {
                using (var context = new CandidateProfileDBContext())
                {
                    context.Entry(candidate).State = candidate.CandidateId == 0 ? EntityState.Added : EntityState.Modified;

                    context.SaveChanges();

                    //clear the cache once add/update candidate value
                    ObjectCache cache = MemoryCache.Default;
                    if (cache.Contains(Cache_Key_Candidates))
                    {
                        cache.Remove(Cache_Key_Candidates);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, new { id = candidate.CandidateId });
                }
            }
            catch (Exception ex)
            {
                //log error message
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        [HttpDelete]
        public void DeleteCandidate(int id)
        {
            try
            {
                using (var context = new CandidateProfileDBContext())
                {
                    Candidate candidate = context.Candidates.Find(id);
                    if (candidate != null)
                    {
                        context.Candidates.Remove(candidate);
                        context.SaveChanges();

                        //clear the cache once delete candidate value
                        ObjectCache cache = MemoryCache.Default;
                        if (cache.Contains(Cache_Key_Candidates))
                        {
                            cache.Remove(Cache_Key_Candidates);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //log error message
            }
        }

        [HttpPost]
        public HttpResponseMessage UploadFile()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            try
            {
                //Check if Request contains any File or not
                if (HttpContext.Current.Request.Files.Count == 0)
                {
                    throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                }

                //Read the File data from Request.Form collections.
                HttpPostedFile uploadedFile = HttpContext.Current.Request.Files[0];
                int CandidateId = Convert.ToInt32(HttpContext.Current.Request.Form["id"]);
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {

                    int MaxContentLength = 1024 * 1024 * 2; //Size = 2 MB  

                    IList<string> AllowedFileExtensions = new List<string> { ".txt", ".csv", ".doc", ".docx" };
                    var ext = uploadedFile.FileName.Substring(uploadedFile.FileName.LastIndexOf('.'));
                    var extension = ext.ToLower();
                    if (!AllowedFileExtensions.Contains(extension))
                    {
                        var message = string.Format("Please Upload file of type .txt,.csv,.doc(x).");

                        dict.Add("error", message);
                        return Request.CreateResponse(HttpStatusCode.BadRequest, dict);
                    }
                    else if (uploadedFile.ContentLength > MaxContentLength)
                    {
                        var message = string.Format("Please Upload a file upto 2 mb.");

                        dict.Add("error", message);
                        return Request.CreateResponse(HttpStatusCode.BadRequest, dict);
                    }
                    else
                    {
                        //Convert the File data to Byte Array which will be store in database
                        byte[] bytes;
                        using (BinaryReader br = new BinaryReader(uploadedFile.InputStream))
                        {
                            bytes = br.ReadBytes(uploadedFile.ContentLength);
                        }

                        //Insert the File to Database Table - FileInfo.

                        using (var context = new CandidateProfileDBContext())
                        {
                            //context.Entry(profileDocument).State = profileDocument.CandidateId == 0 ? EntityState.Added : EntityState.Modified;

                            ProfileDocument profileDocument = context.ProfileDocuments.Find(CandidateId);
                            if (profileDocument != null)
                            {
                                profileDocument.DocumentName = Path.GetFileName(uploadedFile.FileName);
                                profileDocument.DocumentData = bytes;
                                profileDocument.DocumentType = uploadedFile.ContentType;

                                context.Entry(profileDocument).State = EntityState.Modified;
                            }
                            else
                            {
                                profileDocument = new ProfileDocument()
                                {
                                    DocumentName = Path.GetFileName(uploadedFile.FileName),
                                    DocumentData = bytes,
                                    DocumentType = uploadedFile.ContentType,
                                    CandidateId = CandidateId
                                };

                                context.Entry(profileDocument).State = EntityState.Added;
                            }

                            context.SaveChanges();

                            //clear the cache once delete candidate value
                            ObjectCache cache = MemoryCache.Default;
                            if (cache.Contains(Cache_Key_Candidates))
                            {
                                cache.Remove(Cache_Key_Candidates);
                            }

                            return Request.CreateResponse(HttpStatusCode.OK, new { id = profileDocument.CandidateId, Name = profileDocument.DocumentName });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var res = string.Format("upload failed");
                dict.Add("error", res);
                return Request.CreateResponse(HttpStatusCode.NotFound, dict);
            }

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        [HttpGet]
        public HttpResponseMessage GetFile(int candidateId)
        {
            //Create HTTP Response.
            HttpResponseMessage http_Response = Request.CreateResponse(HttpStatusCode.OK);

            //Get the File data from Database based on File ID.
            using (var context = new CandidateProfileDBContext())
            {
                ProfileDocument profileDocument = context.ProfileDocuments.Find(candidateId);
                if (profileDocument != null)
                {
                    HttpResponseMessage httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);
                    httpResponseMessage.Content = new ByteArrayContent(profileDocument.DocumentData);
                    httpResponseMessage.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
                    httpResponseMessage.Content.Headers.ContentDisposition.FileName = profileDocument.DocumentName;
                    httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(profileDocument.DocumentType);

                    return httpResponseMessage;
                }

                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
        }
    }
}