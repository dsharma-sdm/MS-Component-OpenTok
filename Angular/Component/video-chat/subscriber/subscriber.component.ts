import {
  Component,
  ElementRef,
  AfterViewInit,
  ViewChild,
  Input
} from "@angular/core";
import * as OT from "@opentok/client";

@Component({
  selector: "app-subscriber",
  templateUrl: "./subscriber.component.html",
  styleUrls: ["./subscriber.component.css"]
})
export class SubscriberComponent implements AfterViewInit {
  @ViewChild("subscriberDiv") subscriberDiv: ElementRef;
  @Input() session: OT.Session;
  @Input() stream: OT.Stream;
  @Input() index: number;
  constructor() {}

  ngAfterViewInit() {
    const subscriber = this.session.subscribe(
      this.stream,
      this.subscriberDiv.nativeElement,
      {
        insertMode: "append",
        width: "700px",
        height: "500px",
        showControls: true
      },
      err => {
        if (err) {
          console.log(err.message);
          //alert(err.message);
        }
      }
    );
    // let subscribe = document.getElementsByClassName("app-subscriber");
    // for(var i=0;i<subscribe.length;i++)
    //   {
    //     var otSub=subscribe[i].getElementsByClassName("");
    //   }
  }
}
