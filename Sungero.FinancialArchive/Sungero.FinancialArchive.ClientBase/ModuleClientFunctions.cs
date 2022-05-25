using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.FinancialArchive.Client
{
  public class ModuleFunctions
  {
    #region Поиск по гиперссылкам
    
    /// <summary>
    /// Найти бухгалтерский документ.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="incomingTaxInvoice">СФ полученные.</param>
    /// <param name="outgoingTaxInvoice">СФ выставленные.</param>
    /// <param name="contractStatement">Акты.</param>
    /// <param name="waybill">Накладные.</param>
    /// <param name="universalTransferDocument">УПД.</param>
    public void FindAccountingDocuments(string number, string date,
                                        string butin, string butrrc,
                                        string cuuid, string ctin, string ctrrc,
                                        string corrective,
                                        bool incomingTaxInvoice,
                                        bool outgoingTaxInvoice,
                                        bool contractStatement,
                                        bool waybill,
                                        bool universalTransferDocument)
    {
      var result = Functions.Module.Remote.FindAccountingDocuments(number, date,
                                                                   butin, butrrc,
                                                                   cuuid, ctin, ctrrc,
                                                                   corrective == "true",
                                                                   incomingTaxInvoice,
                                                                   outgoingTaxInvoice,
                                                                   contractStatement,
                                                                   waybill,
                                                                   universalTransferDocument);
      
      if (!result.Any())
        Dialogs.ShowMessage("Документ не найден.");
      else if (result.Count == 1)
        result.First().Show();
      else
        result.Show();
    }
    
    /// <summary>
    /// Найти СФ выставленный.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindOutgoingTaxInvoice(string number, string date,
                                       string butin, string butrrc,
                                       string cuuid, string ctin, string ctrrc,
                                       string corrective, string sysid)
    {
      this.FindAccountingDocuments(number, date,
                                   butin, butrrc,
                                   cuuid, ctin, ctrrc,
                                   corrective,
                                   false, true, false, false, true);
    }
    
    /// <summary>
    /// Найти СФ полученный.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindIncomingTaxInvoice(string number, string date,
                                       string butin, string butrrc,
                                       string cuuid, string ctin, string ctrrc,
                                       string corrective, string sysid)
    {
      this.FindAccountingDocuments(number, date,
                                   butin, butrrc,
                                   cuuid, ctin, ctrrc,
                                   corrective,
                                   true, false, false, false, true);
    }
    
    /// <summary>
    /// Найти входящие акты.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindIncomingContractStatement(string number, string date,
                                              string butin, string butrrc,
                                              string cuuid, string ctin, string ctrrc,
                                              string corrective, string sysid)
    {
      // Для накладных и актов корректировки нет. Оставляем управление в RX на будущее.
      corrective = "false";
      
      this.FindAccountingDocuments(number, date,
                                   butin, butrrc,
                                   cuuid, ctin, ctrrc,
                                   corrective,
                                   false, false, true, false, true);
    }
    
    /// <summary>
    /// Найти исходящие акты.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindOutgoingContractStatement(string number, string date,
                                              string butin, string butrrc,
                                              string cuuid, string ctin, string ctrrc,
                                              string corrective, string sysid)
    {
      // Для накладных и актов корректировки нет. Оставляем управление в RX на будущее.
      corrective = "false";
      
      this.FindAccountingDocuments(number, date,
                                   butin, butrrc,
                                   cuuid, ctin, ctrrc,
                                   corrective,
                                   false, false, true, false, true);
    }
    
    /// <summary>
    /// Найти входящие накладные.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindIncomingWayBill(string number, string date,
                                    string butin, string butrrc,
                                    string cuuid, string ctin, string ctrrc,
                                    string corrective, string sysid)
    {
      // Для накладных и актов корректировки нет. Оставляем управление в RX на будущее.
      corrective = "false";
      
      this.FindAccountingDocuments(number, date,
                                   butin, butrrc,
                                   cuuid, ctin, ctrrc,
                                   corrective,
                                   false, false, false, true, true);
    }
    
    /// <summary>
    /// Найти исходящие накладные.
    /// </summary>
    /// <param name="number">Номер.</param>
    /// <param name="date">Дата.</param>
    /// <param name="butin">ИНН НОР.</param>
    /// <param name="butrrc">КПП НОР.</param>
    /// <param name="cuuid">Uuid контрагента.</param>
    /// <param name="ctin">ИНН контрагента.</param>
    /// <param name="ctrrc">КПП контрагента.</param>
    /// <param name="corrective">Признак "Корректировочный".</param>
    /// <param name="sysid">Код инстанса 1С.</param>
    [Hyperlink]
    public void FindOutgoingWayBill(string number, string date,
                                    string butin, string butrrc,
                                    string cuuid, string ctin, string ctrrc,
                                    string corrective, string sysid)
    {
      // Для накладных и актов корректировки нет. Оставляем управление в RX на будущее.
      corrective = "false";
      
      this.FindAccountingDocuments(number, date,
                                   butin, butrrc,
                                   cuuid, ctin, ctrrc,
                                   corrective,
                                   false, false, false, true, true);
    }
    
    #endregion
    
    #region Импорт формализованных документов
    
    /// <summary>
    /// Показать диалог импорта формализованного документа.
    /// </summary>
    [Public]
    public virtual void ImportAndShowDocumentFromFileDialog()
    {
      var dialog = Dialogs.CreateInputDialog(Resources.ImportDialog_Title);
      var fileSelector = dialog.AddFileSelect(Resources.ImportDialog_File, true);
      var import = dialog.Buttons.AddCustom(Resources.ImportDialog_Import);
      dialog.HelpCode = Constants.Module.HelpCodes.Import;
      dialog.Buttons.AddCancel();
      dialog.SetOnButtonClick(b =>
                              {
                                if (b.Button == import && b.IsValid)
                                {
                                  try
                                  {
                                    var xml = Docflow.Structures.Module.ByteArray.Create(fileSelector.Value.Content);
                                    Functions.Module.ImportFormalizedDocument(xml, true).Show();
                                  }
                                  catch (AppliedCodeException ae)
                                  {
                                    b.AddError(ae.Message, fileSelector);
                                  }
                                  catch (Exception ex)
                                  {
                                    var error = Resources.ImportDialog_CannotImportDocument;
                                    Logger.Error(error, ex);
                                    b.AddError(error, fileSelector);
                                  }
                                }
                              });
      dialog.Show();
    }
    
    /// <summary>
    /// Импортировать формализованный документ.
    /// </summary>
    /// <param name="file">Путь к XML файлу.</param>
    /// <param name="requireFtsId">Требовать корректно заполненных данных по участникам сервиса обмена.</param>
    /// <returns>Документ.</returns>
    /// <remarks>Работает с локальными путями клиента, не для веб-клиента.</remarks>
    [Public]
    public virtual Docflow.IAccountingDocumentBase ImportFormalizedDocument(string file, bool requireFtsId)
    {
      var content = System.IO.File.ReadAllBytes(file);
      var array = Docflow.Structures.Module.ByteArray.Create(content);
      return Functions.Module.ImportFormalizedDocument(array, requireFtsId);
    }
    
    /// <summary>
    /// Импортировать формализованный документ.
    /// </summary>
    /// <param name="xml">XML.</param>
    /// <param name="requireFtsId">Требовать корректно заполненных данных по участникам сервиса обмена.</param>
    /// <returns>Документ.</returns>
    [Public]
    public virtual Docflow.IAccountingDocumentBase ImportFormalizedDocument(Docflow.Structures.Module.IByteArray xml, bool requireFtsId)
    {
      var importResult = Functions.Module.Remote.ImportFormalizedDocument(xml, requireFtsId);
      if (!importResult.IsSuccess)
        throw AppliedCodeException.Create(importResult.Error);
      
      var document = importResult.Document;

      // Вызвать обработчик изменения свойства, чтобы создалась связь с корректируемым документом
      // (с сервера несохранённые связи не передаются для оптимизации).
      if (ClientApplication.ApplicationType == ApplicationType.Desktop)
        document.Corrected = document.Corrected;
      
      var memory = new System.IO.MemoryStream(importResult.Body);
      var publicMemory = new System.IO.MemoryStream(importResult.PublicBody);
      
      var version = document.LastVersion;
      version.Body.Write(memory);
      version.PublicBody.Write(publicMemory);
      
      return document;
    }

    #endregion
  }
}