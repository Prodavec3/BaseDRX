-- Заполнить свойство AllowAcquaintanceBySubstitute значением по умолчанию.
update sungero_recman_recmansetting
set allowacqbysub = 'false'
where allowacqbysub is null