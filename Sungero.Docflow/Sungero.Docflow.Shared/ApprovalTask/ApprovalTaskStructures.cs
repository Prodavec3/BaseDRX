using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ApprovalTask
{
  /// <summary>
  /// Признаки невалидной подписи, с текстом ошибки.
  /// </summary>
  partial class SignatureValidationErrors
  {
    public bool IsInvalidCertificate { get; set; }
    
    public bool IsInvalidAttributes { get; set; }
    
    public bool IsInvalidData { get; set; }
  }
  
  /// <summary>
  /// Сотрудник согласует документ в рамках задачи.
  /// </summary>
  partial class ApprovalStatus
  {
    /// <summary>
    /// Есть этап согласования, т.е. согласует.
    /// </summary>
    public bool HasApprovalStage { get; set; }
    
    /// <summary>
    /// Требуется строгая подпись.
    /// </summary>
    public bool NeedStrongSign { get; set; }
    
  }

  /// <summary>
  /// Информация по заданию для предметного отображения.
  /// </summary>
  partial class StateViewAssignmentInfo
  {
    public string PerformerShortName { get; set; }
    
    public string Deadline { get; set; }
    
    public string Status { get; set; }
  }

  /// <summary>
  /// Замещение.
  /// </summary>
  partial class Substitution
  {
    public IRecipient User { get; set; }
    
    public IRecipient SubstitutedUser { get; set; }
  }
  
  partial class BeforeSign
  {
    public List<string> Errors { get; set; }
    
    public bool CanApprove { get; set; }
    
    public bool DocumentBodyChanged { get; set; }
    
  }
  
  partial class ExchangeServies
  {
    public List<ExchangeCore.IExchangeService> Services { get; set; }
    
    public ExchangeCore.IExchangeService DefaultService { get; set; }
  }
  
  /// <summary>
  /// Информация по базовым этапам для обновления формы задачи на согласование по регламенту.
  /// </summary>
  partial class RefreshParameters
  {
    public bool HasDocumentAndCanRead { get; set; }
    
    public bool ForwardPerformerIsVisible { get; set; }
    
    public bool SignatoryIsVisible { get; set; }
    
    public bool SignatoryIsRequired { get; set; }
    
    public bool AddresseeIsEnabled { get; set; }
    
    public bool AddresseeIsVisible { get; set; }
    
    public bool AddresseeIsRequired { get; set; }
    
    public bool AddresseesIsEnabled { get; set; }
    
    public bool AddresseesIsVisible { get; set; }
    
    public bool AddresseesIsRequired { get; set; }
    
    public bool DeliveryMethodIsEnabled { get; set; }
    
    public bool DeliveryMethodIsVisible { get; set; }
    
    public bool ExchangeServiceIsEnabled { get; set; }
    
    public bool ExchangeServiceIsVisible { get; set; }
    
    public bool ExchangeServiceIsRequired { get; set; }
    
    public bool ApproversActionIsEnabled { get; set; }
    
    public bool ApproversIsVisible { get; set; }
    
    public bool AddApproversIsVisible { get; set; }
  }
  
  partial class ReworkParameters
  {
    public bool AllowChangeReworkPerformer { get; set; }
    
    public bool AllowViewReworkPerformer { get; set; }
    
    public bool AllowSendToRework { get; set; }
  }
}