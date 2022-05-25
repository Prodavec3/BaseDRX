-- Установить свойство IsAutoExec в False.
update sungero_wf_task
set IsAutoExec_RecMan_Sungero = 'false'
where IsAutoExec_RecMan_Sungero is null
  and Discriminator = 'c290b098-12c7-487d-bb38-73e2c98f9789'