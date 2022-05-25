using System;

namespace Sungero.ExchangeCore.Constants
{
  public static class BoxBase
  {
    public const string DisableSaveValidation = "DisableSaveValidation";
    
    [Sungero.Core.PublicAttribute]
    public const string JobRunned = "JobRunned";
    
    [Sungero.Core.PublicAttribute]
    public const int DefaultDeadlineInHours = 4;
    
    [Sungero.Core.PublicAttribute]
    public const int MaxDeadline = 100;
  }
}