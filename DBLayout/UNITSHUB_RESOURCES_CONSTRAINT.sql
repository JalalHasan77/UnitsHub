--------------------------------------------------------
--  Constraints for Table UNITSHUB_RESOURCES
--------------------------------------------------------

  ALTER TABLE "INAP"."UNITSHUB_RESOURCES" ADD CHECK (Is_Enabled IN ('Y','N')) ENABLE;
  ALTER TABLE "INAP"."UNITSHUB_RESOURCES" MODIFY ("RESOURCE_TYPE" NOT NULL ENABLE);
  ALTER TABLE "INAP"."UNITSHUB_RESOURCES" MODIFY ("RESOURCE_NAME" NOT NULL ENABLE);
