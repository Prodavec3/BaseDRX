using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchiveUI.Client
{
  public class ModuleFunctions
  {

    /// <summary>
    /// Импорт формализованного документа из файла.
    /// </summary>
    public virtual void ImportAndShowDocumentFromFile()
    {
      FinancialArchive.PublicFunctions.Module.ImportAndShowDocumentFromFileDialog();
    }
    
    /// <summary>
    /// Поиск документов по реквизитам.
    /// </summary>
    public virtual void SearchByRequisites()
    {
      var query = Docflow.PublicFunctions.Module.FinancialDocumentDialogSearch();
      if (query != null)
        query.Show();
    }
    
    /// <summary>
    /// Поиск документов по реквизитам и их выгрузка.
    /// </summary>
    public virtual void SearchByRequisitesAndExport()
    {
      Docflow.PublicFunctions.Module.ExportFinancialDocumentDialogWithSearch();
    }
    
    /// <summary>
    /// Создать финансовый документ.
    /// </summary>
    public virtual void CreateFinancialDocument()
    {
      Docflow.AccountingDocumentBases.CreateDocumentWithCreationDialog(FinancialArchive.ContractStatements.Info,
                                                                       FinancialArchive.IncomingTaxInvoices.Info,
                                                                       FinancialArchive.OutgoingTaxInvoices.Info,
                                                                       FinancialArchive.Waybills.Info,
                                                                       FinancialArchive.UniversalTransferDocuments.Info);
    }
  }
}