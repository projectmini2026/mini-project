import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EcComponent } from './ec-component';

describe('EcComponent', () => {
  let component: EcComponent;
  let fixture: ComponentFixture<EcComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EcComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EcComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
