--------------------------------------------------------
--  Constraints for Table UNITSHUB_USERPERMISSIONS
--------------------------------------------------------

  ALTER TABLE "INAP"."UNITSHUB_USERPERMISSIONS" ADD CHECK (Allow_Deny IN ('A','D')) ENABLE;
