-- Привязать все сохраненные автотексты поручений к платформенному контролу ActiveText.
update Sungero_System_AutotextStatistics
set PropertyId = 'd74c0b99-b7ca-480f-bb05-0fccf92a47b6'
where PropertyId = '11f0499b-7583-4f3e-9975-ee4eb9d7f61b'
  and EntityId = 'c290b098-12c7-487d-bb38-73e2c98f9789'