using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using Nop.Core.Domain.Messages;
using Nop.Core.Infrastructure;
using Nop.Services.Media;
using SparkPostCore;

namespace Nop.Services.Messages
{
    /// <summary>
    /// Email sender
    /// </summary>
    public partial class EmailSender : IEmailSender
    {
        #region Fields

        private readonly IDownloadService _downloadService;
        private readonly INopFileProvider _fileProvider;

        #endregion

        #region Ctor

        public EmailSender(IDownloadService downloadService,
            INopFileProvider fileProvider)
        {
            this._downloadService = downloadService;
            this._fileProvider = fileProvider;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends an email
        /// </summary>
        /// <param name="emailAccount">Email account to use</param>
        /// <param name="subject">Subject</param>
        /// <param name="body">Body</param>
        /// <param name="fromAddress">From address</param>
        /// <param name="fromName">From display name</param>
        /// <param name="toAddress">To address</param>
        /// <param name="toName">To display name</param>
        /// <param name="replyTo">ReplyTo address</param>
        /// <param name="replyToName">ReplyTo display name</param>
        /// <param name="bcc">BCC addresses list</param>
        /// <param name="cc">CC addresses list</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <param name="attachedDownloadId">Attachment download ID (another attachment)</param>
        /// <param name="headers">Headers</param>
        public virtual void SendEmail(EmailAccount emailAccount, string subject, string body,
            string fromAddress, string fromName, string toAddress, string toName,
             string replyTo = null, string replyToName = null,
            IEnumerable<string> bcc = null, IEnumerable<string> cc = null,
            string attachmentFilePath = null, string attachmentFileName = null,
            int attachedDownloadId = 0, IDictionary<string, string> headers = null)
        {


            //create the file attachment for this e-mail message
            if (!string.IsNullOrEmpty(attachmentFilePath) &&
                _fileProvider.FileExists(attachmentFilePath))
            {
                var attachment = new System.Net.Mail.Attachment(attachmentFilePath);
                attachment.ContentDisposition.CreationDate = _fileProvider.GetCreationTime(attachmentFilePath);
                attachment.ContentDisposition.ModificationDate = _fileProvider.GetLastWriteTime(attachmentFilePath);
                attachment.ContentDisposition.ReadDate = _fileProvider.GetLastAccessTime(attachmentFilePath);
                if (!string.IsNullOrEmpty(attachmentFileName))
                {
                    attachment.Name = attachmentFileName;
                }

                //message.Attachments.Add(attachment);
            }
            //another attachment?
            if (attachedDownloadId > 0)
            {
                var download = _downloadService.GetDownloadById(attachedDownloadId);
                if (download != null)
                {
                    //we do not support URLs as attachments
                    if (!download.UseDownloadUrl)
                    {
                        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : download.Id.ToString();
                        fileName += download.Extension;

                        var ms = new MemoryStream(download.DownloadBinary);
                        var attachment = new System.Net.Mail.Attachment(ms, fileName);

                        // handle attachment here

                    }
                }
            }

          
            var transmission = new Transmission();
            transmission.Content.From.Email = fromAddress;
            transmission.Content.Subject = subject;
            transmission.Content.Text = body;

            // add recipients who will receive your email
            var recipient = new Recipient
            {
                Address = new Address { Email = toAddress }
            };

            //BCC
            if (bcc != null)
            {
                foreach (var address in bcc.Where(bccValue => !string.IsNullOrWhiteSpace(bccValue)))
                {                   
                    transmission.Recipients.Add(new Recipient
                    {
                        Address = new Address { Email = address.Trim() }
                    });

                    transmission.Content.Headers.Add("CC", address.Trim());
                }
            }

            //CC
            if (cc != null)
            {
                foreach (var address in cc.Where(ccValue => !string.IsNullOrWhiteSpace(ccValue)))
                {
                    transmission.Recipients.Add(new Recipient
                    {
                        Address = new Address { Email = address.Trim(), HeaderTo = "header_to" }
                    });

                }
            }


            transmission.Recipients.Add(recipient);

            // create a new API client using your API key
            var client = new Client("64d143984ab2071c10d2e1f85fb668a9bdc97dd2");

            // if you do not understand async/await, use the sync sending mode:

            client.CustomSettings.SendingMode = SendingModes.Sync;

            var response = client.Transmissions.Send(transmission);
        }

       

        #endregion
    }
}