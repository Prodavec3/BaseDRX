using System;
using System.Collections.Generic;
using System.Linq;
using CommonLibrary;
using Sungero.Contracts.IncomingInvoice;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Contracts.Server
{
  partial class IncomingInvoiceFunctions
  {
    /// <summary>
    /// Получить дубли входящего счета.
    /// </summary>
    /// <param name="incomingInvoice">Счет для проверки.</param>
    /// <param name="documentKind">Вид счета.</param>
    /// <param name="number">Номер счета.</param>
    /// <param name="date">Дата счета.</param>
    /// <param name="totalAmount">Сумма счета.</param>
    /// <param name="currency">Валюта счета.</param>
    /// <param name="counterparty">Контрагент счета.</param>
    /// <returns>Счета, дублирующие текущий.</returns>
    [Remote(IsPure = true)]
    public static IQueryable<IIncomingInvoice> GetDuplicates(IIncomingInvoice incomingInvoice,
                                                             Sungero.Docflow.IDocumentKind documentKind,
                                                             string number,
                                                             DateTime? date,
                                                             double? totalAmount,
                                                             Commons.ICurrency currency,
                                                             Parties.ICounterparty counterparty)
    {
      return IncomingInvoices.GetAll()
        .Where(i => i.DocumentKind.Equals(documentKind))
        .Where(i => i.Number == number)
        .Where(i => i.Date == date)
        .Where(i => i.TotalAmount == totalAmount)
        .Where(i => i.Currency.Equals(currency))
        .Where(i => i.Counterparty.Equals(counterparty))
        .Where(i => !Equals(i, incomingInvoice));
    }

    /// <summary>
    /// Сводка по документу.
    /// </summary>
    /// <returns>Сводка.</returns>
    public override StateView GetDocumentSummary()
    {
      var documentSummary = StateView.Create();
      var block = documentSummary.AddBlock();
      
      // Краткое имя документа.
      var documentName = _obj.DocumentKind.Name;
      if (!string.IsNullOrWhiteSpace(_obj.RegistrationNumber))
        documentName += Docflow.OfficialDocuments.Resources.Number + _obj.RegistrationNumber;
      
      if (_obj.RegistrationDate != null)
        documentName += Docflow.OfficialDocuments.Resources.DateFrom + _obj.RegistrationDate.Value.ToString("d");
      
      block.AddLabel(documentName);
      block.AddLineBreak();
      block.AddEmptyLine();
      
      // НОР.
      block.AddLabel(string.Format("{0}: ", _obj.Info.Properties.BusinessUnit.LocalizedName));
      if (_obj.BusinessUnit != null)
        block.AddLabel(Hyperlinks.Get(_obj.BusinessUnit));
      else
        block.AddLabel("-");
      
      block.AddLineBreak();
      
      // Контрагент.
      block.AddLabel(string.Format("{0}: ", _obj.Info.Properties.Counterparty.LocalizedName));
      if (_obj.Counterparty != null)
      {
        block.AddLabel(Hyperlinks.Get(_obj.Counterparty));
        if (_obj.Counterparty.Nonresident == true)
          block.AddLabel(string.Format("({0})", _obj.Counterparty.Info.Properties.Nonresident.LocalizedName).ToLower());
      }
      else
      {
        block.AddLabel("-");
      }
      
      block.AddLineBreak();
      
      // Содержание.
      var subject = !string.IsNullOrEmpty(_obj.Subject) ? _obj.Subject : "-";
      block.AddLabel(string.Format("{0}: {1}", _obj.Info.Properties.Subject.LocalizedName, subject));
      block.AddLineBreak();
      
      // Сумма.
      var amount = this.GetTotalAmountDocumentSummary(_obj.TotalAmount, _obj.Currency);
      var amountText = string.Format("{0}: {1}", _obj.Info.Properties.TotalAmount.LocalizedName, amount);
      block.AddLabel(amountText);
      block.AddLineBreak();
      
      // Оплатить до.
      var paymentDeadline = _obj.PaymentDueDate.HasValue ? _obj.PaymentDueDate.Value.ToString("d") : "-";
      var paymentDeadlineText = string.Format("{0}: {1}", _obj.Info.Properties.PaymentDueDate.LocalizedName, paymentDeadline);
      block.AddLabel(paymentDeadlineText);
      block.AddLineBreak();
      block.AddEmptyLine();
      
      // Примечание.
      var note = !string.IsNullOrEmpty(_obj.Note) ? _obj.Note : "-";
      block.AddLabel(string.Format("{0}: {1}", _obj.Info.Properties.Note.LocalizedName, note));
      
      return documentSummary;
    }
    
    /// <summary>
    /// Изменить статус документа на "В разработке".
    /// </summary>
    public override void SetLifeCycleStateDraft()
    {
      if (_obj.LifeCycleState == null)
      {
        Logger.DebugFormat("UpdateLifeCycleState: Document {0} changed LifeCycleState to 'Draft'.", _obj.Id);
        _obj.LifeCycleState = Docflow.OfficialDocument.LifeCycleState.Draft;
      }
    }
    
    /// <summary>
    /// Установить статус жизненного цикла входящего счета в "Оплачен".
    /// </summary>
    public virtual void SetLifeCycleStateToPaid()
    {
      if (_obj.LifeCycleState == Sungero.Contracts.IncomingInvoice.LifeCycleState.Paid)
        return;
      
      Logger.DebugFormat("Update LifeCycleState for document {0}. Current state: {1}, new state: Paid.", _obj.Id, _obj.LifeCycleState);
      _obj.LifeCycleState = Sungero.Contracts.IncomingInvoice.LifeCycleState.Paid;
    }
  }
}