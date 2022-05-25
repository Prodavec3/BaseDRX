--Задача на исполнения поручений. Установить срок соисполнителям такой же как у исполнителя.
update [Sungero_WF_Task]
set [CoAsgDeadlnTAI_RecMan_Sungero] = [DeadlineTAI_RecMan_Sungero]
where [Discriminator] = 'c290b098-12c7-487d-bb38-73e2c98f9789' 
  and [IsCompound_RecMan_Sungero] = 0 
  and [DeadlineTAI_RecMan_Sungero] is not null
  and [CoAsgDeadlnTAI_RecMan_Sungero] is null
  and exists (select 1 from [Sungero_RecMan_TAICoAssignees] as CoAssignees where CoAssignees.[Task] = [Sungero_WF_Task].[Id])