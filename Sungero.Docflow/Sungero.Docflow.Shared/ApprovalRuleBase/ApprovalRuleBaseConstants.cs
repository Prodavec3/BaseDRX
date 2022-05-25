namespace Sungero.Docflow.Constants
{
  public static class ApprovalRuleBase
  {
    /// <summary>
    /// Класс битовых масок для хинтов. Исключительно степени 2.
    /// </summary>
    public static class HintMask
    {
      // Регистратор может регистрировать все виды документов.
      public const int CanRegister = 1;
      
      // Имеются задачи в работе с этим правилом.
      public const int HasTaskInProcess = 2;
    }

    public const string CanRegister = "CanRegister";
    public const string HasTasksInProcess = "HasTasksInProcess";
    public const string CanEditSchema = "CanEditSchema";
    public const string IsSupportConditions = "IsSupportConditions";
    public const string HintsInfoParam = "HintsInfo";
    
    // Срок доработки в днях.
    public const int ReworkDeadline = 3;
    // Максимальный срок доработки в днях.
    public const int MaxReworkDeadline = 36500;    
  }
}