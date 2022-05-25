using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.OutgoingDocumentBase;

namespace Sungero.Docflow.Client
{
  partial class OutgoingDocumentBaseFunctions
  {
    /// <summary>
    /// Диалог выбора формата конверта.
    /// </summary>
    /// <param name="outgoingDocuments">Список исходящих документов.</param>
    /// <param name="contractualDocuments">Список договорных документов.</param>
    public static void ShowSelectEnvelopeFormatDialog(List<IOutgoingDocumentBase> outgoingDocuments, List<IContractualDocumentBase> contractualDocuments)
    {
      ShowSelectEnvelopeFormatDialog(outgoingDocuments, contractualDocuments, null);
    }
    
    /// <summary>
    /// Диалог выбора формата конверта.
    /// </summary>
    /// <param name="outgoingDocuments">Список исходящих документов.</param>
    /// <param name="contractualDocuments">Список договорных документов.</param>
    /// /// <param name="accountingDocuments">Список финансовых документов.</param>
    public static void ShowSelectEnvelopeFormatDialog(List<IOutgoingDocumentBase> outgoingDocuments, List<IContractualDocumentBase> contractualDocuments, List<IAccountingDocumentBase> accountingDocuments)
    {
      if (outgoingDocuments == null)
        outgoingDocuments = new List<IOutgoingDocumentBase>();
      
      if (contractualDocuments == null)
        contractualDocuments = new List<IContractualDocumentBase>();
      
      if (accountingDocuments == null)
        accountingDocuments = new List<IAccountingDocumentBase>();
      
      var resources = OutgoingDocumentBases.Resources;
      var defaultEnvelopeFormat = resources.DLEnvelope.ToString();
      var defaultPrintSender = true;
      
      // Из персональных настроек взять формат конверта и необходимость печати отправителя.
      var personalSetting = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Company.Employees.Current);
      if (personalSetting != null)
      {
        defaultEnvelopeFormat = personalSetting.EnvelopeFormat.HasValue ?
          PersonalSettings.Info.Properties.EnvelopeFormat.GetLocalizedValue(personalSetting.EnvelopeFormat.Value) :
          resources.DLEnvelope.ToString();
        defaultPrintSender = personalSetting.PrintSender ?? true;
      }
      
      // Диалог выбора отчета.
      var dialog = Dialogs.CreateInputDialog(resources.EnvelopePrinting);
      dialog.HelpCode = Constants.OutgoingDocumentBase.EnvelopeDialogHelpCode;
      dialog.Buttons.AddOkCancel();
      var envelopeFormat = dialog.AddSelect(resources.EnvelopeFormat, true, defaultEnvelopeFormat)
        .From(resources.DLEnvelope, resources.C4Envelope, resources.C5Envelope, resources.C65Envelope);
      var needPrintSender = dialog.AddBoolean(resources.NeedPrintSender, defaultPrintSender);
      
      if (dialog.Show() != DialogButtons.Ok)
        return;
      
      // Выбрать отчет в зависимости от указанного формата.
      if (envelopeFormat.Value == resources.DLEnvelope)
      {
        var report = Docflow.Reports.GetEnvelopeE65Report();
        report.PrintSender = needPrintSender.Value;
        report.OutgoingDocuments.AddRange(outgoingDocuments);
        report.ContractualDocuments.AddRange(contractualDocuments);
        report.AccountingDocuments.AddRange(accountingDocuments);
        report.Open();
      }
      else if (envelopeFormat.Value == resources.C4Envelope)
      {
        var report = Docflow.Reports.GetEnvelopeC4Report();
        report.PrintSender = needPrintSender.Value;
        report.OutgoingDocuments.AddRange(outgoingDocuments);
        report.ContractualDocuments.AddRange(contractualDocuments);
        report.AccountingDocuments.AddRange(accountingDocuments);
        report.Open();
      }
      else if (envelopeFormat.Value == resources.C5Envelope)
      {
        var report = Docflow.Reports.GetEnvelopeC5Report();
        report.PrintSender = needPrintSender.Value;
        report.OutgoingDocuments.AddRange(outgoingDocuments);
        report.ContractualDocuments.AddRange(contractualDocuments);
        report.AccountingDocuments.AddRange(accountingDocuments);
        report.Open();
      }
      else if (envelopeFormat.Value == resources.C65Envelope)
      {
        var report = Docflow.Reports.GetEnvelopeC65Report();
        report.PrintSender = needPrintSender.Value;
        report.OutgoingDocuments.AddRange(outgoingDocuments);
        report.ContractualDocuments.AddRange(contractualDocuments);
        report.AccountingDocuments.AddRange(accountingDocuments);
        report.Open();
      }
    }
    
    /// <summary>
    /// Получить текст для отметки документа устаревшим.
    /// </summary>
    /// <returns>Текст для диалога прекращения согласования.</returns>
    public override string GetTextToMarkDocumentAsObsolete()
    {
      return OutgoingDocumentBases.Resources.MarkDocumentAsObsolete;
    }
    
    /// <summary>
    /// Получить признак доступности на изменение таблицы адресатов.
    /// </summary>
    /// <param name="e">Аргументы события.</param>
    /// <returns>True - если необходимо запретить изменение, иначе - false.</returns>
    [Public]
    public bool DisableAddresseesOnRegistration(Sungero.Domain.Shared.BaseEventArgs e)
    {
      bool repeatRegister;
      var changeRegistrationRequisites = e.Params.TryGetValue(Sungero.Docflow.Constants.OfficialDocument.RepeatRegister, out repeatRegister) && repeatRegister;
      var isRegistered = _obj != null && _obj.RegistrationState != RegistrationState.NotRegistered;
      return isRegistered && !changeRegistrationRequisites;
    }
  }
}