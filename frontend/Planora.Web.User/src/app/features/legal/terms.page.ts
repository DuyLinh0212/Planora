import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlanoraLogoComponent } from '../../shared/planora-logo.component';

@Component({
  selector: 'app-terms-page',
  imports: [RouterLink, PlanoraLogoComponent],
  templateUrl: './terms.page.html',
  styleUrl: './terms.page.css',
})
export class TermsPage {}
