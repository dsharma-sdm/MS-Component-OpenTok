import { AppService } from "./../../../../../../app-service.service";
import { MatDialog } from "@angular/material";
import { AddNewCallerComponent } from "./../../../../../../shared/add-new-caller/add-new-caller.component";
import { Router, ActivatedRoute } from "@angular/router";
import { CommonService } from "./../../../../core/services/common.service";
import {
  Component,
  ElementRef,
  AfterViewInit,
  ViewChild,
  Input,
  OnDestroy,
  ViewEncapsulation,
} from "@angular/core";
import { OpentokService } from "../../opentok.service";
import { ChatInitModel } from "src/app/shared/models/chatModel";
import { TextChatService } from "src/app/shared/text-chat/text-chat.service";
import { VideoRecordModel } from "src/app/shared/models/videoRecordModel";
import { CallInitModel, CallStatus } from "src/app/shared/models/callModel";
import { EncounterService } from "../../encounter.service";

@Component({
  selector: "app-publisher",
  templateUrl: "./publisher.component.html",
  styleUrls: ["./publisher.component.css"],
  encapsulation: ViewEncapsulation.None,
})
export class PublisherComponent implements AfterViewInit, OnDestroy {
  @ViewChild("publisherDiv") publisherDiv: ElementRef;
  @Input() session: OT.Session;
  @Input() patientAppointmentId: number;
  @Input() otSessionId: number;
  publisher: OT.Publisher;
  publishing: Boolean;
  isScreenShare: boolean;
  isVideo: boolean = true;
  isVideoBtn: boolean = false;
  isFullScreen: boolean = false;
  isProvider: boolean = false;
  dateTimeNow: Date;
  screenSize: Array<any> = [
    { id: 1, size: "1:8", class: "one-forth-width" },
    { id: 2, size: "1:4", class: "half-width" },
    { id: 3, size: "1:2", class: "" },
    { id: 4, size: "1:1", class: "video-call-fixed" },
  ];
  screenId: any;
  isVideoRecord: boolean = false;
  archiveId: string = "";
  constructor(
    private opentokService: OpentokService,
    private commonService: CommonService,
    private router: Router,
    private activatedRoute: ActivatedRoute,
    private dialogModal: MatDialog,
    private appService: AppService,
    private textChatService: TextChatService,
    private encounterService: EncounterService
  ) {
    this.publishing = false;
    if (localStorage.getItem("access_token")) {
      this.commonService.userRole.subscribe((res) => {
        if (res.toUpperCase() === "PROVIDER") {
          this.isProvider = true;
        }
      });
    }
    this.screenId = this.screenSize[2];
    //console.log(this.screenId);
    this.archiveId = "";
    this.dateTimeNow = new Date();
  }
  ngOnInit() {
    this.appService.videoRecordingStarted.subscribe(
      (videoRecordModel: VideoRecordModel) => {
        this.isVideoRecord = videoRecordModel.isRecording;
        this.archiveId = "";
        if (videoRecordModel.isRecording)
          this.archiveId = videoRecordModel.archiveId;
      }
    );
  }
  // ngAfterViewInit() {
  //   this.startVideoCall();
  // }

  // cycleVideo() {
  //   this.publisher && this.publisher.cycleVideo();
  // }

  // toggleScreen() {
  //   if (this.isScreenShare)
  //     this.startVideoCall();
  //   else
  //     this.screenshare()
  //   this.isScreenShare = !this.isScreenShare;
  // }

  // startVideoCall() {
  //   const OT = this.opentokService.getOT();
  //   this.publisher = OT.initPublisher(this.publisherDiv.nativeElement,
  //     { name: 'OpenTok', style: {}, insertMode: 'append', width: '70px', height: '50px', showControls: true, },
  //     (err) => {
  //       err && alert(err.message);
  //     });

  //   if (this.session) {
  //     if (this.session['isConnected']()) {
  //       this.publish();
  //     }
  //     this.session.on('sessionConnected', () => this.publish());
  //   }
  // }

  // screenshare() {
  //   const OT = this.opentokService.getOT();
  //   OT.checkScreenSharingCapability((response) => {
  //     if (!response.supported || response.extensionRegistered === false) {
  //       alert('This browser does not support screen sharing.');
  //     } else if (response.extensionInstalled === false) {
  //       alert('Please install the screen sharing extension and load your app over https.');
  //     } else {
  //       // Screen sharing is available. Publish the screen.
  //       this.publisher = OT.initPublisher(this.publisherDiv.nativeElement, {width: '70px',  height: '50px', showControls: true, videoSource: 'window' });
  //       if (this.session) {
  //         if (this.session['isConnected']()) {
  //           this.publish();
  //         }
  //         this.session.on('sessionConnected', () => this.publish());
  //       }
  //     }
  //   })
  // }

  // publish() {
  //   this.session.publish(this.publisher, (err) => {
  //     if (err) {
  //       alert(err.message);
  //     } else {
  //       this.publishing = true;
  //     }
  //   });
  // }

  // ngOnDestroy() {
  //   if (!this.isScreenShare)
  //     this.session.disconnect();
  // }

  ngAfterViewInit() {
    this.startVideoCall();
  }

  cycleVideo() {
    this.publisher && this.publisher.cycleVideo();
  }

  toggleVideo(video: boolean) {
    this.isVideoBtn = false;
    if (video == true) {
      this.publisher.publishVideo(false);
      this.publisher.publishAudio(true);
      this.isVideo = false;
    } else {
      this.publisher.publishVideo(true);
      this.publisher.publishAudio(true);
      this.isVideo = true;
    }
  }
  toggleFullScreen(isFullScreen: boolean) {
    this.isFullScreen = !isFullScreen;
    let videoTool = document.getElementsByClassName("video-call");
    videoTool[0].classList.toggle("video-call-fixed");
  }
  toggleScreen() {
    if (this.isScreenShare) this.startVideoCall();
    else this.screenshare();
    this.isScreenShare = !this.isScreenShare;
  }
  screenshare() {
    const OT = this.opentokService.getOT();
    OT.checkScreenSharingCapability((response) => {
      if (!response.supported || response.extensionRegistered === false) {
        alert("This browser does not support screen sharing.");
      } else if (response.extensionInstalled === false) {
        alert(
          "Please install the screen sharing extension and load your app over https."
        );
      } else {
        // Screen sharing is available. Publish the screen.
        this.publisher = OT.initPublisher(this.publisherDiv.nativeElement, {
          width: "70px",
          height: "50px",
          showControls: true,
          videoSource: "window",
        });
        if (this.session) {
          if (this.session["isConnected"]()) {
            this.publish();
          }
          this.session.on("sessionConnected", () => this.publish());
        }
      }
    });
  }
  endCall() {
    debugger;
    var apptId = 0;
    this.activatedRoute.queryParams.subscribe((param) => {
      apptId = param["apptId"];
    });
    localStorage.removeItem("otSession");
    this.commonService.isPatient.subscribe((isPatient) => {
      if (isPatient) {
        this.session.on("streamDestroyed", (e) => e.preventDefault());
        this.session.disconnect();
        this.router.navigate(["/web/encounter/thank-you"], {
          queryParams: {
            apptId: apptId,
          },
        });
      } else {
        this.session.on("streamDestroyed", (e) => e.preventDefault());
        this.session.disconnect();
      }
    });
    let videoCall = document.getElementsByClassName("video-call");
    videoCall[0].classList.add("video-call-hide");
    let videoTool = document.getElementsByClassName("video-tool");
    videoTool[0].classList.add("video-tool-hide");
    let callInitModel: CallInitModel = new CallInitModel();
    callInitModel.AppointmentId = 0;
    callInitModel.CallStatus = CallStatus.Over;
    this.appService.CheckCallStarted(callInitModel);
    this.updateEndCall();
  }

  updateEndCall(){

      this.encounterService
        .UpdateTelehealthCallDuration(this.otSessionId, this.dateTimeNow)
        .subscribe((response) => {
          if (response.statusCode == 200) {
            console.log("Call ended", response.data);
          }
        });

  }

  startVideoCall() {
    const OT = this.opentokService.getOT();
    this.publisher = OT.initPublisher(
      this.publisherDiv.nativeElement,
      {
        name: "OpenTok",
        style: {},
        insertMode: "append",
        width: "70px",
        height: "50px",
        showControls: true,
      },
      (err) => {
        err && console.log(err.message); //alert(err.message);
      }
    );

    if (this.session) {
      if (this.session["isConnected"]()) {
        this.publish();
      }
      this.session.on("sessionConnected", () => this.publish());
      this.session.on("sessionDisconnected", function (event) {});
    }
  }

  publish() {
    this.session.publish(this.publisher, (err) => {
      if (err) {
        console.log(err.message);
        //alert(err.message);
      } else {
        this.publishing = true;
      }
    });
  }
  addNewCaller() {
    let dbModal;
    dbModal = this.dialogModal.open(AddNewCallerComponent, {
      data: {
        appointmentId: this.patientAppointmentId,
        sessionId: this.otSessionId,
      },
    });
    dbModal.afterClosed().subscribe((result: string) => {
      if (result == "save") {
      }
    });
  }
  startChat() {
    var chatInitModel = new ChatInitModel();
    var response = JSON.parse(
      this.commonService.encryptValue(localStorage.getItem("otSession"), false)
    );
    if (!localStorage.getItem("access_token")) {
      chatInitModel.isActive = true;
      chatInitModel.AppointmentId = response.appointmentId;
      chatInitModel.UserId = response.userId;
      chatInitModel.UserRole = "Invited";
    } else {
      this.commonService.userRole.subscribe((role) => {
        if (role != "PROVIDER") {
          this.commonService.loginUser.subscribe((loginDetail: any) => {
            if (loginDetail.access_token) {
              chatInitModel.isActive = true;
              chatInitModel.AppointmentId = response.appointmentId;
              chatInitModel.UserId = loginDetail.data.userID;
              chatInitModel.UserRole =
                loginDetail.data.users1.userRoles.userType;
            }
          });
        } else {
          this.commonService.loginUser.subscribe((loginDetail: any) => {
            if (loginDetail.access_token) {
              chatInitModel.isActive = true;
              chatInitModel.AppointmentId = response.appointmentId;
              chatInitModel.UserId = loginDetail.data.userID;
              chatInitModel.UserRole = loginDetail.data.userRoles.userType;
            }
          });
        }
      });
    }
    this.appService.CheckChatActivated(chatInitModel);
    // this.getAppointmentInfo(
    //   chatInitModel.AppointmentId,
    //   chatInitModel.UserRole
    // );
    this.textChatService.setAppointmentDetail(
      chatInitModel.AppointmentId,
      chatInitModel.UserRole
    );
    this.textChatService.setRoomDetail(
      chatInitModel.UserId,
      chatInitModel.AppointmentId
    );
  }
  setScreenSize(event: any) {
    this.screenId = event.value;
    let videoTool = document.getElementsByClassName("video-tool");
    if (videoTool != undefined && videoTool.length > 0) {
      videoTool[0].classList.remove("video-call-fixed");
      videoTool[0].classList.remove("half-width");
      videoTool[0].classList.remove("one-forth-width");
      var className = this.screenId.class;
      //console.log(className);
      videoTool[0].classList.add(className);
    }
  }
  startStopCallRecording() {
    this.isVideoRecord = !this.isVideoRecord;
    if (this.isVideoRecord) {
      var response = JSON.parse(
        this.commonService.encryptValue(
          localStorage.getItem("otSession"),
          false
        )
      );
      this.otSessionId = response.id;
      const config = {
        API_KEY: +response.apiKey,
        TOKEN: response.token,
        SESSION_ID: response.sessionID,
        SAMPLE_SERVER_BASE_URL: "",
      };
      this.appService
        .startVideoRecording(response.sessionID)
        .subscribe((response) => {
          //console.log(response);
        });
    } else if (this.archiveId != "") {
      this.appService
        .stopVideoRecording(this.archiveId, this.patientAppointmentId)
        .subscribe((response) => {
          //console.log(response);
        });
    }
  }

  ngOnDestroy() {
    this.session.disconnect();
  }
}
