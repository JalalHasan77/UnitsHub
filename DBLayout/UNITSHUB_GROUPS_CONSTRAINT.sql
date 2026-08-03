--------------------------------------------------------
--  Constraints for Table UNITSHUB_GROUPS
--------------------------------------------------------

  ALTER TABLE "INAP"."UNITSHUB_GROUPS" ADD CHECK (Is_System IN ('Y','N')) ENABLE;
  ALTER TABLE "INAP"."UNITSHUB_GROUPS" MODIFY ("GROUP_NAME" NOT NULL ENABLE);
