using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.VerificationAssignment;

namespace Sungero.SmartProcessing.Server
{
  partial class VerificationAssignmentFunctions
  {
    /// <summary>
    /// Передать ресурс с инструкцией.
    /// </summary>
    /// <returns>Ресурс.</returns>
    [Remote(IsPure = true)]
    public StateView GetInstruction()
    {
      // Текст инструкции разделен на несколько ресурсов для того,
      // чтобы по умолчанию текст в хидере не сворачивался до нескольких видимых первых строк
      // и расположенной ниже надписи "Показать еще", а был сразу виден полностью.
      var instruction = StateView.Create();
      var block = instruction.AddBlock();
      block.AddLabel(VerificationAssignments.Resources.InstructionStep1);
      block.AddLineBreak();
      block.AddLabel(VerificationAssignments.Resources.InstructionStep2);
      block.AddLineBreak();
      block.AddLabel(VerificationAssignments.Resources.InstructionStep3);
      block.AddLineBreak();
      block.AddLabel(VerificationAssignments.Resources.InstructionStep4);
      block.AddLineBreak();
      block.AddLabel(VerificationAssignments.Resources.InstructionStep5);
      block.AddLineBreak();
      block.AddLabel(VerificationAssignments.Resources.InstructionStep6);
      block.AddLineBreak();
      block.AddLabel(VerificationAssignments.Resources.InstructionStep7);
      
      return instruction;
    }
  }
}