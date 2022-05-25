--Обновление задачи на исполнение поручения.

-- Установить свойство "Бессрочное" в задаче на исполнение поручения в False.
update t
set HasIndefinitDL_RecMan_Sungero = 'false'
from Sungero_WF_Task t 
where HasIndefinitDL_RecMan_Sungero is null
  and Discriminator = 'c290b098-12c7-487d-bb38-73e2c98f9789'
  
--Обновление GUID для пунктов составного поручения.

-- Присвоить свойству "Уникальный идентификатор пункта поручения" значение нового GUID.
update Sungero_RecMan_TAIParts
set PartGuid = NEWID()
where PartGuid is null