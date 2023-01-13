using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalTask;
using Sungero.Workflow;

namespace Sungero.Docflow.Client
{
  partial class ApprovalTaskFunctions
  {
    
    #region Проверка на прочитанность документа

    /// <summary>
    /// Запросить подтверждение на выполнение задания.
    /// </summary>
    /// <param name="attachedDocument">Документ.</param>
    /// <param name="confirmationMessage">Сообщение с подтверждением.</param>
    /// <param name="dialogId">Ид диалога.</param>
    /// <param name="isSigning">Признак того, что документ проходит этап подписания.</param>
    /// <returns>True, если запрос был подтверждён.</returns>
    /// <remarks>Делает remote запрос.</remarks>
    public static bool ConfirmCompleteAssignment(IOfficialDocument attachedDocument, string confirmationMessage, string dialogId, bool isSigning = false)
    {
      var changed = Functions.ApprovalTask.Remote.DocumentHasBodyUpdateAfterLastView(attachedDocument);
      var viewed = Functions.ApprovalTask.Remote.DocumenHasBeenViewed(attachedDocument);
      return ConfirmCompleteAssignment(changed, viewed, confirmationMessage, dialogId, isSigning);
    }
    
    /// <summary>
    /// Запросить подтверждение на выполнение задания.
    /// </summary>
    /// <param name="documentBodyChanged">Признак непрочитанного документа.</param>
    /// <param name="documentViewed">Признак того, что документ не был просмотрен с момента создания.</param>
    /// <param name="confirmationMessage">Сообщение с подтверждением.</param>
    /// <param name="dialogId">Ид диалога.</param>
    /// <param name="isSigning">Признак того, что документ проходит этап подписания.</param>
    /// <returns>True, если запрос был подтверждён.</returns>
    public static bool ConfirmCompleteAssignment(bool documentBodyChanged, bool documentViewed, string confirmationMessage, string dialogId, bool isSigning = false)
    {
      // Если описание не задано, то диалог будет выглядеть как обычный диалог подтверждения выполнения задания.
      string description = null;
      if (!documentViewed)
        description = isSigning
          ? ApprovalTasks.Resources.NeedViewDocumentBeforeAgreeAtLeastOnceBeforeSigning
          : ApprovalTasks.Resources.NeedViewDocumentBeforeAgreeAtLeastOnceBeforeApproving;
      else if (documentBodyChanged)
        description = ApprovalTasks.Resources.NeedViewActualDocumentVersionBeforeAgree;
      return Docflow.Functions.Module.ShowConfirmationDialog(confirmationMessage, description, null, dialogId);
    }
    
    #endregion
    
    #region Электронная подпись
    
    /// <summary>
    /// Показать ошибку в хинте с требованием усиленной подписи.
    /// </summary>
    /// <param name="assignment">Задание, в котором показывается хинт.</param>
    /// <param name="e">Аргументы действия.</param>
    public static void ShowStrongSignErrorHint(IAssignment assignment, Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.AddError(ApprovalTasks.Resources.CertificateNeeded);
    }
    
    /// <summary>
    /// Проверить возможность отправки задания подписания на доработку.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <param name="eventArgs">Аргумент обработчика вызова.</param>
    /// <returns>True - разрешить отправку, иначе false.</returns>
    public static bool ValidateBeforeRework(IAssignment assignment, string errorMessage, Sungero.Domain.Client.ExecuteActionArgs eventArgs)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(ApprovalTasks.As(assignment.Task)))
      {
        eventArgs.AddError(ApprovalTasks.Resources.NoRightsToDocument);
        return false;
      }
      if (string.IsNullOrWhiteSpace(assignment.ActiveText))
      {
        eventArgs.AddError(errorMessage);
        return false;
      }
      
      if (!eventArgs.Validate())
        return false;
      
      return true;
    }
    
    #endregion

    /// <summary>
    /// Проверить, требуется ли скрытие создания сопроводительного письма.
    /// </summary>
    /// <param name="collapsedStageTypes">Схлопнутые этапы.</param>
    /// <returns>True, если требуется.</returns>
    public bool NeedHideCoverLetterAction(List<Enumeration?> collapsedStageTypes)
    {
      if (!collapsedStageTypes.Where(s => s == Docflow.ApprovalStage.StageType.Sending).Any())
        return true;
      
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      if (ContractualDocumentBases.Is(document) || IncomingDocumentBases.Is(document))
        return false;
      
      return true;
    }

    /// <summary>
    /// Вывод диалога запроса причины прекращения задачи согласования.
    /// </summary>
    /// <param name="activeText">Причина прекращения.</param>
    /// <param name="e">Аргумент события.</param>
    /// <param name="fromTask">Признак того, что проверка запускается из задачи.</param>
    /// <returns>True, если пользователь нажал Ok.</returns>
    public bool GetReasonBeforeAbort(string activeText, Sungero.Domain.Client.ExecuteActionArgs e, bool fromTask)
    {
      var dialog = Dialogs.CreateInputDialog(ApprovalTasks.Resources.Confirmation);
      var abortingReason = dialog.AddMultilineString(_obj.Info.Properties.AbortingReason.LocalizedName, true, activeText);
      CommonLibrary.IBooleanDialogValue isDocumentClosed = null;
      IOfficialDocument document = null;

      if (Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
      {
        document = _obj.DocumentGroup.OfficialDocuments.First();
        var textToMarkDocumentAsObsolete = Functions.OfficialDocument.GetTextToMarkDocumentAsObsolete(document);
        var defaultMarkDocumentAsObsoleteValue = Functions.OfficialDocument.MarkDocumentAsObsolete(document);
        isDocumentClosed = dialog.AddBoolean(textToMarkDocumentAsObsolete, defaultMarkDocumentAsObsoleteValue);
      }
      
      dialog.SetOnButtonClick(args =>
                              {
                                if (string.IsNullOrWhiteSpace(abortingReason.Value))
                                  args.AddError(ApprovalTasks.Resources.EmptyAbortingReason, abortingReason);
                                
                                if (fromTask)
                                {
                                  var actualModified = Functions.ApprovalTask.Remote.GetApprovalTaskModified(_obj);
                                  if (!Equals(_obj.Modified, actualModified))
                                  {
                                    if (isDocumentClosed != null)
                                      isDocumentClosed.IsEnabled = false;
                                    args.AddError(ApprovalTasks.Resources.CantUpdateTask);
                                  }
                                }
                              });
      
      if (dialog.Show() == DialogButtons.Ok)
      {
        _obj.AbortingReason = abortingReason.Value;
        if (isDocumentClosed != null && isDocumentClosed.Value.Value == true)
        {
          var isActive = document.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Active;
          var isDraft = document.LifeCycleState == Sungero.Docflow.OfficialDocument.LifeCycleState.Draft;
          
          if (isActive || isDraft || document.LifeCycleState == null)
            ((Domain.Shared.IExtendedEntity)_obj).Params[Constants.ApprovalTask.NeedSetDocumentObsolete] = true;
        }
        return true;
      }
      return false;
    }
    
    /// <summary>
    /// Возможность создавать сопроводительное письмо к документу.
    /// </summary>
    /// <param name="document">Официальный документ.</param>
    /// <returns>True, если можно создать сопроводительное письмо.</returns>
    public static bool EnableCreateCoverLetter(IOfficialDocument document)
    {
      return document.DocumentKind.DocumentFlow == Docflow.DocumentKind.DocumentFlow.Contracts;
    }

    /// <summary>
    /// Показать хинт о доступности сервиса обмена на событии изменения значения контрола.
    /// </summary>
    /// <param name="state">Свойство.</param>
    /// <param name="info">Информация о свойстве.</param>
    /// <param name="deliveryMethod">Сервис обмена.</param>
    /// <param name="e">Аргументы события Изменение значения контрола.</param>
    public void ShowExchangeHint(Domain.Shared.IPropertyState state, Domain.Shared.IPropertyInfo info, IMailDeliveryMethod deliveryMethod, Sungero.Presentation.ValueInputEventArgs<IMailDeliveryMethod> e)
    {
      if (this.NeedShowExchangeHint(state, info, deliveryMethod, e.Params))
        e.AddInformation(info, Sungero.Docflow.ApprovalTasks.Resources.ExchangeDeliveryExist, _obj.Info.Properties.ApprovalRule);
    }
    
    /// <summary>
    /// Показать хинт о доступности сервиса обмена на событии обновления формы.
    /// </summary>
    /// <param name="state">Свойство.</param>
    /// <param name="info">Информация о свойстве.</param>
    /// <param name="deliveryMethod">Сервис обмена.</param>
    /// <param name="e">Аргументы события Обновление формы.</param>
    public void ShowExchangeHint(Domain.Shared.IPropertyState state, Domain.Shared.IPropertyInfo info, IMailDeliveryMethod deliveryMethod, Sungero.Domain.Shared.BaseEntityValidationEventArgs e)
    {
      if (this.NeedShowExchangeHint(state, info, deliveryMethod, e.Params))
        e.AddInformation(info, Sungero.Docflow.ApprovalTasks.Resources.ExchangeDeliveryExist, _obj.Info.Properties.ApprovalRule);
    }

    /// <summary>
    /// Узнать, нужно ли показывать хинт о доступности сервиса обмена.
    /// </summary>
    /// <param name="state">Свойство.</param>
    /// <param name="info">Информация о свойстве.</param>
    /// <param name="deliveryMethod">Сервис обмена.</param>
    /// <param name="param">Параметр, в котором хранится информация о необходимости показать хинт.</param>
    /// <returns>Признак необходимости показать хинт. True - если нужно показать хинт, иначе - false.</returns>
    public bool NeedShowExchangeHint(Domain.Shared.IPropertyState state, Domain.Shared.IPropertyInfo info, IMailDeliveryMethod deliveryMethod, Domain.Shared.ParamsDictionary param)
    {
      var isVisibleAndEnabled = state.IsVisible && state.IsEnabled;
      if (isVisibleAndEnabled && (deliveryMethod == null || deliveryMethod.Sid != Constants.MailDeliveryMethod.Exchange))
      {
        var show = false;
        if (!param.TryGetValue(Constants.ApprovalTask.NeedShowExchangeServiceHint, out show))
        {
          show = Functions.ApprovalTask.Remote.GetExchangeServices(_obj).DefaultService != null;
          param.AddOrUpdate(Constants.ApprovalTask.NeedShowExchangeServiceHint, show);
        }
        
        return show;
      }
      return false;
    }
    
    /// <summary>
    /// Вызвать диалог продления срока задания.
    /// </summary>
    /// <param name="oldDeadline">Старый срок.</param>
    /// <returns>Новый срок в случае нажатия кнопки "Продлить", иначе null.</returns>
    public static DateTime? GetNewDeadline(DateTime? oldDeadline)
    {
      var dialog = Dialogs.CreateInputDialog(ApprovalTasks.Resources.DeadlineExtension);
      dialog.HelpCode = Constants.Module.HelpCodes.DeadlineExtensionDialog;
      var newDeadline = dialog.AddDate(ApprovalTasks.Resources.NewDeadline, true).AsDateTime();
      newDeadline.Value = oldDeadline < Calendar.Now ? Calendar.Now.AddWorkingDays(Users.Current, 3) : oldDeadline.Value.AddWorkingDays(Users.Current, 3);
      
      dialog.Buttons.AddCustom(ApprovalTasks.Resources.DeadlineExtensionButton);
      dialog.Buttons.AddCancel();
      dialog.SetOnButtonClick((args) =>
                              {
                                if (!Functions.Module.CheckDeadline(newDeadline.Value, oldDeadline))
                                  args.AddError(ApprovalTasks.Resources.DesiredDeadlineIsNotCorrect);
                              });
      
      if (dialog.Show() != DialogButtons.Cancel)
        return newDeadline.Value;
      return null;
    }
    
    /// <summary>
    /// Показать диалог выбора исполнителя доработки с запросом выдачи прав на вложения.
    /// </summary>
    /// <param name="assignment">Задание.</param>
    /// <param name="attachments">Вложения.</param>
    /// <param name="additionalAssignees">Дополнительные согласующие.</param>
    /// <param name="reworkPerformer">Ответственный за доработку.</param>
    /// <param name="e">Аргументы.</param>
    /// <param name="dialogId">Ид диалога.</param>
    public virtual void ShowReworkConfirmationDialog(IAssignmentBase assignment, List<Domain.Shared.IEntity> attachments,
                                                     List<IRecipient> additionalAssignees, Sungero.Company.IEmployee reworkPerformer,
                                                     Sungero.Workflow.Client.ExecuteResultActionArgs e, string dialogId)
    {
      if (reworkPerformer != null)
        additionalAssignees.Add(reworkPerformer);
      
      // Диалог выдачи прав (отображается, если нет прав на вложения).
      if (Functions.Module.ShowDialogGrantAccessRightsWithConfirmationDialog(_obj, attachments, additionalAssignees, e.Action, dialogId) == false)
        e.Cancel();
    }
    
    /// <summary>
    /// Показать хинт при прекращении задачи на согласование.
    /// </summary>
    public virtual void AbortAsyncProcessingNotify()
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
        return;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      var needGrantAccessRightsOnDocumentAsync = Functions.ApprovalTask.NeedGrantAccessRightsOnDocument(_obj);
      var needSetStateAsync = !document.AccessRights.CanUpdate();
      if (needSetStateAsync && needGrantAccessRightsOnDocumentAsync)
        Dialogs.NotifyMessage(Sungero.Docflow.ApprovalTasks.Resources.DocumentStateAndAccessRightsWillBeUpdatedLater);
      else if (needSetStateAsync)
        Dialogs.NotifyMessage(Sungero.Docflow.ApprovalTasks.Resources.DocumentStateWillBeUpdatedLater);
      else if (needGrantAccessRightsOnDocumentAsync)
        Dialogs.NotifyMessage(Sungero.Docflow.ApprovalTasks.Resources.DocumentAccessRightsWillBeUpdatedLater);
    }
    
    /// <summary>
    /// Получить ошибку согласования основного документа.
    /// </summary>
    /// <param name="needStrongSign">Признак того, что согласование требует усиленную подпись.</param>
    /// <returns>Текст ошибки. Null - если ошибки нет.</returns>
    public virtual CommonLibrary.LocalizedString GetPrimaryDocumentApproveValidationError(bool needStrongSign)
    {
      if (!Functions.ApprovalTask.HasDocumentAndCanRead(_obj))
        return ApprovalTasks.Resources.NoRightsToDocument;
      
      var document = _obj.DocumentGroup.OfficialDocuments.First();
      if (document.HasVersions && needStrongSign && !PublicFunctions.Module.Remote.GetCertificates(document).Any())
        return ApprovalTasks.Resources.CertificateNeeded;
      return null;
    }
  }
}