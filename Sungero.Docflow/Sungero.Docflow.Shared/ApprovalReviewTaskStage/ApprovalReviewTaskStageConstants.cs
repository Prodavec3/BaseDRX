using System;
using Sungero.Core;

namespace Sungero.Docflow.Constants
{
  public static class ApprovalReviewTaskStage
  {
    /// <summary>
    /// Срок ожидания по умолчанию для этапа рассмотрения несколькими адресатами в часах.
    /// </summary>
    public const int DefaultTimeoutInHours = 4;
    
    /// <summary>
    /// Срок рассмотрения по умолчанию для этапа рассмотрения несколькими адресатами в днях.
    /// </summary>
    public const int DefaultDeadlineInDays = 3;
    
    /// <summary>
    /// Код для дополнительной информации в ссылке, связывающей задачу на согласование по регламенту с задачей на рассмотрение документа.
    /// </summary>
    public const string ApprovalReviewTaskStageLinkCode = "ApprovalReviewTaskStageLink";
  }
}