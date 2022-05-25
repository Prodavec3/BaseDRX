using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement.DocumentReviewTask;
using Sungero.RecordManagement.Structures.DocumentReviewTask;

namespace Sungero.RecordManagement.Shared
{
  partial class DocumentReviewTaskFunctions
  {
    /// <summary>
    /// Получить сообщения валидации при старте.
    /// </summary>
    /// <returns>Сообщения валидации.</returns>
    public virtual List<StartValidationMessage> GetStartValidationMessages()
    {
      var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
      var errors = new List<StartValidationMessage>();
      bool workingWithGUI = Sungero.Commons.PublicFunctions.Module.EntityParamsContainsKey(_obj, Constants.DocumentReviewTask.WorkingWithGuiParamName);
      
      var authorIsNonEmployeeMessage = Docflow.PublicFunctions.Module.ValidateTaskAuthor(_obj);
      if (!string.IsNullOrWhiteSpace(authorIsNonEmployeeMessage))
        errors.Add(StartValidationMessage.Create(authorIsNonEmployeeMessage, false, true));
      
      // Документ на исполнении нельзя отправлять на рассмотрение.
      if (workingWithGUI && document != null && document.ExecutionState == Docflow.OfficialDocument.ExecutionState.OnExecution)
        errors.Add(StartValidationMessage.Create(DocumentReviewTasks.Resources.DocumentOnExecution, false, false));
      
      // Проверить корректность срока.
      if (_obj.Addressees.Any(x => !Docflow.PublicFunctions.Module.CheckDeadline(x.Addressee, _obj.Deadline, Calendar.Now)))
        errors.Add(StartValidationMessage.Create(RecordManagement.Resources.ImpossibleSpecifyDeadlineLessThanToday, true, false));
      
      // Проверить, что входящий документ зарегистрирован.
      if (workingWithGUI && !Functions.DocumentReviewTask.IncomingDocumentRegistered(document))
        errors.Add(StartValidationMessage.Create(DocumentReviewTasks.Resources.IncomingDocumentMustBeRegistered, false, false));
      
      return errors;
    }
    
    /// <summary>
    /// Валидация старта задачи на рассмотрение.
    /// </summary>
    /// <param name="e">Аргументы действия.</param>
    /// <returns>True, если валидация прошла успешно, и False, если были ошибки.</returns>
    public virtual bool ValidateDocumentReviewTaskStart(Sungero.Core.IValidationArgs e)
    {
      var errorMessages = this.GetStartValidationMessages();
      if (errorMessages.Any())
      {
        foreach (var error in errorMessages)
        {
          if (error.IsCantSendTaskByNonEmployeeMessage)
            e.AddError(_obj.Info.Properties.Author, error.Message);
          else if (error.IsImpossibleSpecifyDeadlineLessThanTodayMessage)
            e.AddError(_obj.Info.Properties.Deadline, error.Message);
          else
            e.AddError(error.Message);
        }
        return false;
      }
      
      return true;
    }
    
    /// <summary>
    /// Проверка, зарегистрирован ли входящий документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>True, если документ зарегистрирован, либо документ не входящий.</returns>
    public static bool IncomingDocumentRegistered(IOfficialDocument document)
    {
      if (document == null || document.DocumentKind == null)
        return true;
      
      var documentKind = document.DocumentKind;
      return documentKind.DocumentFlow != Docflow.DocumentKind.DocumentFlow.Incoming ||
        documentKind.NumberingType != Docflow.DocumentKind.NumberingType.Registrable ||
        document.RegistrationState == Docflow.OfficialDocument.RegistrationState.Registered;
    }
    
    /// <summary>
    /// Проверить, завершена ли задача на рассмотрение.
    /// </summary>
    /// <returns>True, если задача на рассмотрение выполнена, иначе - False.</returns>
    public virtual bool IsDocumentReviewTaskCompleted()
    {
      return Docflow.PublicFunctions.Module.IsTaskCompleted(_obj);
    }
    
    /// <summary>
    /// Получить список просроченных задач на исполнение поручения в состоянии Черновик.
    /// </summary>
    /// <returns>Список просроченных задач на исполнение поручения в состоянии Черновик.</returns>
    public virtual List<IActionItemExecutionTask> GetDraftOverdueActionItemExecutionTasks()
    {
      var tasks = _obj.ResolutionGroup.ActionItemExecutionTasks.Where(t => t.Status == RecordManagement.ActionItemExecutionTask.Status.Draft);
      var overdueTasks = new List<IActionItemExecutionTask>();
      foreach (var task in tasks)
        if (Functions.ActionItemExecutionTask.CheckOverdueActionItemExecutionTask(task))
          overdueTasks.Add(task);
      
      return overdueTasks;
    }
    
    /// <summary>
    /// Синхронизировать адресатов из документа в задачу на рассмотрение руководителем.
    /// </summary>
    /// <param name="document">Документ на рассмотрение.</param>
    [Public]
    public virtual void SynchronizeAddressees(Docflow.IOfficialDocument document)
    {
      var documentAddressees = Docflow.PublicFunctions.OfficialDocument.GetAddressees(document);
      var newAddressees = documentAddressees
        .Except(_obj.Addressees.Select(x => x.Addressee))
        .Where(x => x != null)
        .ToList();
      foreach (var newAddressee in newAddressees)
        _obj.Addressees.AddNew().Addressee = newAddressee;
    }
    
    /// <summary>
    /// Задать адресатов в задаче.
    /// </summary>
    /// <param name="addressees">Адресаты.</param>
    public virtual void SetAddressees(List<IEmployee> addressees)
    {
      _obj.Addressees.Clear();
      if (addressees == null)
        return;
      addressees = addressees.Where(x => x != null).ToList();
      foreach (var addressee in addressees)
        _obj.Addressees.AddNew().Addressee = addressee;
    }
    
    /// <summary>
    /// Установить срок задачи на рассмотрение документа.
    /// </summary>
    /// <param name="days">Срок в днях.</param>
    /// <param name="hours">Срок в часах.</param>
    [Public]
    public virtual void SetDeadline(int? days, int? hours)
    {
      _obj.Deadline = Calendar.Now.AddWorkingDays(_obj.Author, days ?? 0).AddWorkingHours(_obj.Author, hours ?? 0);
    }
    
    /// <summary>
    /// Проверить наличие документа на рассмотрение в задаче и наличие хоть каких-то прав на него.
    /// </summary>
    /// <returns>True, если с документом можно работать.</returns>    
    [Public]
    public virtual bool HasDocumentAndCanRead()
    {
      return _obj.DocumentForReviewGroup.OfficialDocuments.Any();
    }    
  }
}