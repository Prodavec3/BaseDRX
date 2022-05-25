do $$
begin

--Обновление задачи на исполнение поручения.

-- Установить свойство "Бессрочное" в задаче на исполнение поручения в False.
update sungero_wf_task
set hasindefinitdl_recman_sungero = 'false'
where hasindefinitdl_recman_sungero is null
  and discriminator = 'c290b098-12c7-487d-bb38-73e2c98f9789';

--Обновление GUID для пунктов составного поручения.

-- Присвоить свойству "Уникальный идентификатор пункта поручения" значение нового GUID.
update sungero_recman_taiparts
set partguid = uuid_generate_v4()
where partguid is null;

end $$