--------------------------------------------------------
--  Constraints for Table UNITSHUB_USERS
--------------------------------------------------------

  ALTER TABLE "INAP"."UNITSHUB_USERS" ADD CHECK (Is_Enabled IN ('Y','N')) ENABLE;
