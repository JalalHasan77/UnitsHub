--------------------------------------------------------
--  Constraints for Table XUNITSHUB_GROUPS
--------------------------------------------------------

  ALTER TABLE "INAP"."XUNITSHUB_GROUPS" ADD CHECK (Is_System IN ('Y','N')) ENABLE;
  ALTER TABLE "INAP"."XUNITSHUB_GROUPS" MODIFY ("GROUP_NAME" NOT NULL ENABLE);
