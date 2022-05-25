using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.ExchangeCore.BusinessUnitBox;

namespace Sungero.ExchangeCore.Client
{
  partial class BusinessUnitBoxFunctions
  {
    /// <summary>
    /// Отобразить диалог поиска документов из сервисов обмена.
    /// </summary>
    public virtual void ShowExchangeDocumentsSearchDialog()
    {
      var dialog = Dialogs.CreateInputDialog(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.FindDocumentDialogTitle);
      dialog.HelpCode = Constants.BusinessUnitBox.ExchangeDocumentsSearchHelpCode;
      var hyperlink = dialog.AddString(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.FindDocumentDialogHyperlink, true);
      
      var closeButton = dialog.Buttons.AddCustom(Sungero.Docflow.Resources.Dialog_Close);
      var nextButton = dialog.Buttons.AddCustom(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.FindDocumentDialogNext);
      var cancelButton = dialog.Buttons.AddCustom(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.FindDocumentDialogCancel);
      closeButton.IsVisible = false;
      
      var availableDocumentsLink = dialog.AddHyperlink(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.DocumentFullInfoDialogDocuments);
      availableDocumentsLink.IsVisible = false;
      var availableDocuments = new List<Docflow.IOfficialDocument>();
      availableDocumentsLink.SetOnExecute(() =>
                                          {
                                            availableDocuments.ShowModal();
                                          });
      
      var documentInfosLink = dialog.AddHyperlink(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.DocumentFullInfoDialogDocumentInfos);
      documentInfosLink.IsVisible = false;
      var documentInfos = new List<Exchange.IExchangeDocumentInfo>();
      documentInfosLink.SetOnExecute(() =>
                                     {
                                       documentInfos.ShowModal();
                                     });
      
      var messagesLink = dialog.AddHyperlink(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.DocumentFullInfoDialogMessages);
      messagesLink.IsVisible = false;
      var messages = new List<IMessageQueueItem>();
      messagesLink.SetOnExecute(() =>
                                {
                                  messages.ShowModal();
                                });
      
      dialog.SetOnRefresh((arg) =>
                          {
                            availableDocumentsLink.IsVisible = availableDocuments.Any();
                            documentInfosLink.IsVisible = documentInfos.Any();
                            messagesLink.IsVisible = messages.Any();
                          });
      
      dialog.SetOnButtonClick((arg) =>
                              {
                                if (arg.Button == closeButton)
                                {
                                  arg.CloseAfterExecute = true;
                                  return;
                                }
                                
                                if (!arg.IsValid)
                                  return;
                                
                                arg.CloseAfterExecute = false;
                                if (string.IsNullOrWhiteSpace(hyperlink.Value))
                                  arg.AddError(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.FindDocumentDialogInvalidHyperlink);
                                
                                var messageIdParsed = string.Empty;
                                var documentIdParsed = string.Empty;
                                if (_obj.ExchangeService.ExchangeProvider == Sungero.ExchangeCore.ExchangeService.ExchangeProvider.Diadoc)
                                {
                                  messageIdParsed = Functions.Module.GetParameterValueFromHyperlink(hyperlink.Value, Constants.BusinessUnitBox.DocumentHyperlinkParameterLetterId);
                                  documentIdParsed = Functions.Module.GetParameterValueFromHyperlink(hyperlink.Value, Constants.BusinessUnitBox.DocumentHyperlinkParameterDocumentId);
                                }
                                else
                                {
                                  documentIdParsed = Functions.Module.GetDocumentGuidFromHyperlink(hyperlink.Value);
                                  messageIdParsed = documentIdParsed;
                                }
                                if (!Functions.Module.CheckGuid(messageIdParsed) || !Functions.Module.CheckGuid(documentIdParsed))
                                {
                                  arg.AddError(Sungero.ExchangeCore.BusinessUnitBoxes.Resources.FindDocumentDialogInvalidHyperlink);
                                  return;
                                }
                                
                                var exchangePackagesInfo = Functions.BusinessUnitBox.Remote.GetExchangeDocumentsPackage(messageIdParsed, documentIdParsed);
                                var documentsCount = exchangePackagesInfo.Documents != null ? exchangePackagesInfo.Documents.Where(d => d.DocumentId != null).Count() : 0;
                                dialog.Text += BusinessUnitBoxes.Resources.DocumentFullInfoDialogAllDocumentsCountFormat(documentsCount);
                                
                                if (exchangePackagesInfo.Documents != null && exchangePackagesInfo.Documents.Count > 0)
                                {
                                  var forbiddenDocuments = exchangePackagesInfo.Documents.Where(d => d.Document != null && d.HasDocumentReadPermissions != true).Select(d => d.DocumentId).ToList();
                                  if (forbiddenDocuments.Any())
                                  {
                                    var forbiddenDocumentsCount = forbiddenDocuments.Count();
                                    dialog.Text += BusinessUnitBoxes.Resources.DocumentFullInfoDialogForbiddenDocumentsCountFormat(Environment.NewLine, forbiddenDocumentsCount);
                                  }
                                  availableDocuments = exchangePackagesInfo.Documents.Where(d => d.Document != null && d.HasDocumentReadPermissions == true).Select(d => d.Document).ToList();
                                  documentInfos = exchangePackagesInfo.Documents.Where(d => d.DocumentInfo != null).Select(d => d.DocumentInfo).ToList();
                                }
                                
                                if (exchangePackagesInfo.Messages != null)
                                  messages = exchangePackagesInfo.Messages;
                                
                                nextButton.IsVisible = false;
                                cancelButton.IsVisible = false;
                                closeButton.IsVisible = true;
                                hyperlink.IsVisible = false;
                              });
      
      dialog.Show();
    }
  }
}