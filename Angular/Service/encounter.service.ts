import { Injectable, ElementRef } from "@angular/core";
import { CommonService } from "../../core/services";
import { Observable, BehaviorSubject } from "rxjs";
import { AnimationPlayer } from "@angular/animations";

const getQueryParamsFromObject = (filterModal: object): string => {
  let queryParams = "";
  let index = 0;
  for (const key of Object.keys(filterModal)) {
    if (index === 0) queryParams += `?${key}=${filterModal[key]}`;
    else queryParams += `&${key}=${filterModal[key]}`;

    index++;
  }
  return queryParams;
};

@Injectable({
  providedIn: "root"
})
export class EncounterService {
  private changeSOAPPanelStateSubject = new BehaviorSubject<any>({});
  public SOAPPanelData = this.changeSOAPPanelStateSubject.asObservable();

  getMasterDataURL = "api/MasterData/MasterDataByName";
  getPatientEncounterDetailsURL = "patient-encounter/GetPatientEncounterDetails";
  getNonBillableEncounterDetailsURL = "patient-encounter/GetPatientNonBillableEncounterDetails";
  getAppConfigurationsURL = "AppConfigurations/GetAppConfigurations";
  savePatientEncounterURL = "patient-encounter/SavePatientEncounter";
  saveNonBillableEncounterURL = "patient-encounter/SavePatientNonBillableEncounter";
  saveEncounterSignatureURL = "patient-encounter/SaveEncounterSignature";
  createClaimURL = "Claim/CreateClaim";
  private getClientHeaderInfoURL = "Patients/GetPatientHeaderInfo";
  getTelehealthSessionURL = "api/Telehealth/GetTelehealthSession?appointmentId=";
  getMasterTemplatesURL = "MasterTemplates/GetMasterTemplates";
  GetTemplateByIdURL = "patient-encounter/GetPatientEncounterTemplateData";
  SaveTemplateDataURL = "patient-encounter/SaveEncounterTemplateData";
  private getPreviousEncountersURL = "patient-encounter/GetPreviousEncounters";
  private getPreviousEncountersDataURL = "patient-encounter/GetPreviousEncountersData";
  private checkServiceCodeAvailabilityURL = "patient-encounter/CheckServiceCodeAvailability";
  getTelehealthSessionForInvitedAppointmentIdURL = "api/Telehealth/GetTelehealthSessionByInvitedAppointmentId?appointmentId=";
  voiceCall = 'api/PatientAppointments/AppointmentsCall';
  UpdateCallURL = "api/Telehealth/UpdateTelehealthCallDuration";
  private SOAPPanel: ElementRef;
  constructor(private commonService: CommonService) {}

  changeSOAPPanelData(
    changedState: boolean,
    extraParams: any,
    isSaved: boolean = false
  ) {
    this.changeSOAPPanelStateSubject.next({
      changedState,
      ...extraParams,
      isSaved
    });
  }
  get SOAPPanelRef() {
    return this.SOAPPanel;
  }
  CheckServiceCodeAvailability(
    patientAppointmentId: any,
    serviceCodeId: string
  ) {
    const queryParams = `?patientAppointmentId=${patientAppointmentId}&serviceCodeId=${serviceCodeId}`;
    return this.commonService.post(
      this.checkServiceCodeAvailabilityURL + queryParams,
      {}
    );
  }
  public setClientPanel(clientPanel: ElementRef) {
    this.SOAPPanel = clientPanel;
  }
  getMasterData(masterData: any): Observable<any> {
    return this.commonService.post(this.getMasterDataURL, masterData);
  }

  GetPatientEncounterDetails(apptId: any, encId: any, isAdmin: boolean) {
    const queryParams = `?appointmentId=${apptId}&encounterId=${encId}&isAdmin=${isAdmin}`;
    return this.commonService.getAll(
      this.getPatientEncounterDetailsURL + queryParams,
      {}
    );
  }

  getNonBillableEncounterDetails(
    apptId: number,
    encId: number,
    isAdmin: boolean
  ) {
    const queryParams = `/${apptId}/${encId}?isAdmin=${isAdmin}`;
    return this.commonService.getAll(
      this.getNonBillableEncounterDetailsURL + queryParams,
      {}
    );
  }

  getAppConfigurations() {
    return this.commonService.getAll(this.getAppConfigurationsURL, {});
  }

  SavePatientEncounter(postData: any, isAdmin: boolean) {
    const queryParams = `?isAdmin=${isAdmin}`;
    return this.commonService.post(
      this.savePatientEncounterURL + queryParams,
      postData
    );
  }

  saveNonBillableEncounter(postData: any, isAdmin: boolean) {
    const queryParams = `?isAdmin=${isAdmin}`;
    return this.commonService.post(
      this.saveNonBillableEncounterURL + queryParams,
      postData
    );
  }

  saveEncounterSignature(postData: any) {
    return this.commonService.post(this.saveEncounterSignatureURL, postData);
  }

  createClaim(encounterId: number, isAdmin: boolean) {
    const queryParams = `?patientEncounterId=${encounterId}&isAdmin=${isAdmin}`;
    return this.commonService.post(this.createClaimURL + queryParams, {});
  }

  getClientHeaderInfo(id: number, appointmentID: any) {
    return this.commonService.getById(
      this.getClientHeaderInfoURL + "?id=" + id + "&apptId=" + appointmentID,
      {}
    );
  }

  getTelehealthSession(appointmentId: number) {
    //const queryParams = getQueryParamsFromObject(filters);
    return this.commonService.getAll(
      `${this.getTelehealthSessionURL}${appointmentId}`,
      {}
    );
  }
  getTelehealthSessionForInvitedAppointmentId(appointmentId: number) {
    //const queryParams = getQueryParamsFromObject(filters);
    return this.commonService.getAll(
      `${this.getTelehealthSessionForInvitedAppointmentIdURL}${appointmentId}`,
      {}
    );
  }

  getMasterTemplates() {
    return this.commonService.getAll(this.getMasterTemplatesURL, {});
  }

  getTemplateForm(patientEncounterId: any, templateId: any) {
    const queryParams = `?patientEncounterId=${patientEncounterId}&masterTemplateId=${templateId}`;
    return this.commonService.getById(
      this.GetTemplateByIdURL + queryParams,
      {}
    );
  }

  saveTemplateData(postData: any) {
    return this.commonService.post(this.SaveTemplateDataURL, postData);
  }
  getPreviousEncounters(patientId: number, fromDate: string, toDate: string) {
    let url =
      this.getPreviousEncountersURL +
      "?patientId=" +
      patientId +
      "&fromDate=" +
      fromDate +
      "&toDate=" +
      toDate;
    return this.commonService.getAll(url, {});
  }
  getPreviousEncountersData(previousEncounterId: number) {
    let url =
      this.getPreviousEncountersDataURL +
      "?previousEncounterId=" +
      previousEncounterId;
    return this.commonService.getAll(url, {});
  }

  connectWithCall(id: string,DoctorPhoneNumber:any,appointmentId:any) {
    const queryParams = `?patientId=${id}&doctorNumber=${DoctorPhoneNumber}&appointmentId=${appointmentId}`;
    return this.commonService.getById(
      this.voiceCall + queryParams,
      {}
    );
  }

  UpdateTelehealthCallDuration(otSessionId: any, dateTimeNow: any){
    const postData = {
      telehealthSessionDetailID: otSessionId,
      endDateTime:dateTimeNow};
    return this.commonService.post(this.UpdateCallURL, postData);
  }
}
