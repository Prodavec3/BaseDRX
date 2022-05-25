using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.ContractsUI.Client
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Создать документ с диалогом выбора типа документа.
    /// </summary>
    public virtual void CreateDocument()
    {
      Contracts.ContractualDocuments.CreateDocumentWithCreationDialog(Contracts.ContractualDocuments.Info,
                                                                      Docflow.SimpleDocuments.Info,
                                                                      Docflow.Addendums.Info,
                                                                      FinancialArchive.ContractStatements.Info,
                                                                      Contracts.IncomingInvoices.Info,
                                                                      Contracts.OutgoingInvoices.Info,
                                                                      Docflow.CounterpartyDocuments.Info);
    }
    
    /// <summary>
    /// Поиск договорных документов по контрагенту.
    /// </summary>
    /// <returns>Документы, удовлетворяющие условиям.</returns>
    public virtual IQueryable<Contracts.IContractualDocument> SearchContractualDocumentsWithCounterparty()
    {
      var personalSettings = Docflow.PublicFunctions.PersonalSetting.GetPersonalSettings(Company.Employees.Current);

      var dialog = Dialogs.CreateInputDialog(Resources.ContractualDocumentsWithContractors);
      var counterparty = dialog.AddSelect(Resources.Counterparty, true, Parties.Counterparties.Null);
      var periodBegin = dialog.AddDate(Resources.RegistrationDateFrom, false);
      var periodEnd = dialog.AddDate(Resources.RegistrationDateTo, false);
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckDialogPeriod(args, periodBegin, periodEnd);
                              });
      
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      if (dialog.Show() == DialogButtons.Ok)
        return Contracts.PublicFunctions.Module.Remote.GetContractualDocsWithCounterparty(counterparty.Value,
                                                                                          periodBegin.Value, periodEnd.Value);
      
      return null;
    }

    /// <summary>
    /// Поиск договорных документов по регистрационным данным.
    /// </summary>
    /// <returns>Документы, удовлетворяющие условиям.</returns>
    public virtual IQueryable<Contracts.IContractualDocument> SearchByRegistrationData()
    {
      // Создать диалог ввода регистрационных данных документа.
      var dialog = Dialogs.CreateInputDialog(Resources.SearchByRegistrationData);
      var registrationNumber = dialog.AddString(Docflow.Resources.RegistrationNumber, false);
      var registrationDateFrom = dialog.AddDate(Resources.RegistrationDateFrom, false);
      var registrationDateTo = dialog.AddDate(Resources.RegistrationDateTo, false);
      var documentRegister = dialog.AddSelect(Docflow.Resources.DocumentRegister, false, Docflow.DocumentRegisters.Null)
        .From(Contracts.PublicFunctions.Module.Remote.GetContractualDocumentRegisters());
      var fileList = dialog.AddSelect(Resources.CaseFile, false, Docflow.CaseFiles.Null);
      var registeredBy = dialog.AddSelect(Resources.ResponsibleEmployee, false, Company.Employees.Null);
      dialog.Buttons.AddOkCancel();
      dialog.Buttons.Default = DialogButtons.Ok;
      
      dialog.SetOnButtonClick((args) =>
                              {
                                Docflow.PublicFunctions.Module.CheckDialogPeriod(args, registrationDateFrom, registrationDateTo);
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
        return Contracts.PublicFunctions.Module.Remote.GetFilteredRegisteredDocuments(registrationNumber.Value,
                                                                                      registrationDateFrom.Value,
                                                                                      registrationDateTo.Value,
                                                                                      documentRegister.Value,
                                                                                      fileList.Value,
                                                                                      registeredBy.Value);
      return null;
    }

  }
}