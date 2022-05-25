using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Docflow.Structures.ApprovalReviewAssignment
{

  /// <summary>
  /// Подпись и версия документа.
  /// </summary>
  partial class DocumentSignature
  {
    public int SignatureId { get; set; }
    
    public DateTime SigningDate { get; set; }
    
    public int? VersionNumber { get; set; }
  }

  /// <summary>
  /// Список подписей.
  /// </summary>
  partial class SignaturesInfo
  {
    public IUser Signatory { get; set; }
    
    public IUser SubstitutedUser { get; set; }
    
    public string SignatoryType { get; set; }
  }  
}