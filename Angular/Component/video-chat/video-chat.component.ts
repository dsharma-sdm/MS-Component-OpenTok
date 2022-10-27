import { CommonService } from "./../../../core/services/common.service";
import {
  Component,
  ChangeDetectorRef,
  ViewEncapsulation,
  Input,
  OnChanges,
  OnInit,
} from "@angular/core";
import { OpentokService } from "../opentok.service";
import { EncounterService } from "../encounter.service";
import * as OT from "@opentok/client";
import { SchedulerService } from "../../../scheduling/scheduler/scheduler.service";
import * as moment from "moment";

@Component({
  selector: "app-video-chat",
  templateUrl: "./video-chat.component.html",
  styleUrls: ["./video-chat.component.css"],
  encapsulation: ViewEncapsulation.None,
})
export class VideoChatComponent implements OnInit {
  @Input() patientAppointmentDetails: any;
  @Input() patientAppointmentId: any;
  title = "Video Chat";
  session: OT.Session;
  streams: Array<OT.Stream> = [];
  changeDetectorRef: ChangeDetectorRef;
  startTime: Date;
  endTime: Date;
  actualMin: any;
  otSessionId: number = 0;
  constructor(
    private ref: ChangeDetectorRef,
    private opentokService: OpentokService,
    private encounterService: EncounterService,
    private schedulerService: SchedulerService,
    private commonService: CommonService
  ) {
    this.changeDetectorRef = ref;
  }

  ngOnInit() {
    let divVideoTool = document.getElementById("divVideoTool");
    if (divVideoTool != undefined) {
      let videoTool = document.getElementsByClassName("video-tool");
      if (videoTool != undefined && videoTool.length > 0) {
        var position = divVideoTool.getBoundingClientRect();
        videoTool[0].setAttribute("style", "left:" + position.left + "px;");
      }
    }
    //videoTool[0].classList.add("soap-note-video");
    // this.commonService.videoSessionStarted.subscribe(res => {
    //   var a = res;
    //   if (
    //     localStorage.getItem("otSession") &&
    //     localStorage.getItem("otSession") != ""
    //   )
    //   console.log()
    // });
    if (this.patientAppointmentDetails && !localStorage.getItem("otSession")) {
      // const {
      //   patientID,
      //   appointmentStaffs,
      //   endDateTime,
      //   startDateTime
      // } = this.patientAppointmentDetails;
      // const staffId =
      //   appointmentStaffs && appointmentStaffs.length == 1
      //     ? appointmentStaffs[0].staffId
      //     : null;

      // if (staffId && patientID) {
      //   const filters = {
      //     patientID,
      //     staffID: staffId,
      //     startTime: moment(startDateTime).format("YYYY-MM-DDTHH:mm:ss"),
      //     endTime: moment(endDateTime).format("YYYY-MM-DDTHH:mm:ss")
      //   };
      this.encounterService
        .getTelehealthSession(this.patientAppointmentId)
        .subscribe((response) => {
          if (response.statusCode == 200) {
            var otSession = this.commonService.encryptValue(
              JSON.stringify(response.data)
            );
            //let videoTool = document.getElementsByClassName("video-tool");
            //videoTool[0].classList.remove("video-tool-hide");
            localStorage.setItem("otSession", otSession);
            this.commonService.videoSession(true);
            this.otSessionId = response.data.id;
            // const config = {
            //   API_KEY: +response.data.apiKey,
            //   TOKEN: response.data.token,
            //   SESSION_ID: response.data.sessionID,
            //   SAMPLE_SERVER_BASE_URL: ""
            // };
            //this.getSession(config);
          }
        });
    }
    //}
  }

  // getSession(config: any) {
  //   this.opentokService
  //     .initSession(config)
  //     .then((session: OT.Session) => {
  //       console.log(session);
  //       this.session = session;
  //       this.session.on("streamCreated", event => {
  //         this.startTime = new Date();
  //         this.streams.push(event.stream);
  //         this.changeDetectorRef.detectChanges();
  //       });
  //       this.session.on("streamDestroyed", event => {
  //         this.capturedEndTime();
  //         if (event.reason === "networkDisconnected") {
  //           event.preventDefault();
  //           var subscribers = session.getSubscribersForStream(event.stream);
  //           if (subscribers.length > 0) {
  //             var subscriber = document.getElementById(subscribers[0].id);
  //             // Display error message inside the Subscriber
  //             subscriber.innerHTML =
  //               "Lost connection. This could be due to your internet connection " +
  //               "or because the other party lost their connection.";
  //             event.preventDefault(); // Prevent the Subscriber from being removed
  //           }
  //         }
  //         const idx = this.streams.indexOf(event.stream);
  //         if (idx > -1) {
  //           this.streams.splice(idx, 1);
  //           this.changeDetectorRef.detectChanges();
  //         }
  //       });
  //       this.session.on("sessionDisconnected", event => {
  //         this.capturedEndTime();
  //       });
  //     })
  //     .then(() => this.opentokService.connect())
  //     .catch(err => {
  //       console.error("error", err);
  //       alert(
  //         "Unable to connect. Make sure you have updated the config.ts file with your OpenTok details."
  //       );
  //     });
  // }
  private capturedEndTime() {
    this.endTime = new Date();
    this.diff_minutes(this.endTime, this.startTime);
    this.updateCallDuration();
  }

  diff_minutes(dt2: Date, dt1: Date) {
    var diff = (dt2.getTime() - dt1.getTime()) / 1000;
    diff /= 60;
    this.actualMin = Math.abs(Math.round(diff));
  }
  updateCallDuration() {
    const OT = this.opentokService.getOT();
    this.schedulerService
      .UpdateCallDuration(this.patientAppointmentId, this.actualMin)
      .subscribe((response) => {
        if (response.statusCode === 200) {
          console.log("Call time duration sucessfully captured");
        } else {
          console.log("On call duration captured time something went wrong");
        }
      });
  }
}
