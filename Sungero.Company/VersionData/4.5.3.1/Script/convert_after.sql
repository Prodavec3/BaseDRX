--Справочник Ассистенты руководителей. Заполнение свойства "Отправляет поручения от имени руководителя" значением по умолчанию.
update sungero_company_assistant
set sendactionitem = isassistant
where sendactionitem is null;