#include "sys.h"
#include "usart.h"
#include "delay.h"
#include "led.h"
#include "lcd.h"
#include "usmart.h"
#include "ov7670.h"
#include "exti.h"
#include "timer.h"

int freq_mul = 15;
extern u8 ov_sta;
extern u32 ov_frame;
u8 CanSend = 0;

void pixels()
{
	u16 count = 0;
	u16 color;
	__asm
	{
		mov  r6, #320
		mov  r7, #240
		mul  r7, r6
		mov  r3, 0
		mov  r5, 1
		mov  r1, #0x0c00
		movt r1, #0x4001
		mov  r2, #0x0184
		movt r2, #0x4221
		mov  r8, #0x1010
		movt r8, #0x4001
		mov  r9,  #0x88888888
		mov  r10, #0x33333333
		mov  r11, #0x100
		mov  r12, #0x200
		mov  r0, #0x280
		mov  r6, 0
		again:
		str  r9, [r1]                   //GPIOB->CRL=0X88888888;
		str  r3, [r2]                   // OV7670->RCK = 0;
		ldrb r4, [r1, 0x8]              // r4 = GPIOB->IDR;
		str  r5, [r2]                   // OV7670->RCK = 1;
		str  r3, [r2]                   // OV7670->RCK = 0;
		ldrb r6, [r1, 0x8]              // r6 = GPIOB->IDR;
		str  r5, [r2]                   // OV7670->RCK = 1;
		str  r10, [r1]                  //GPIOB->CRL=0X33333333;
		add  r6, r4, LSL #8             // r6+=(r4<<8)
		mov color,r6
	}

	USART1->DR = color>>8;
	while((USART1->SR&0X40)==0);
	USART1->DR = (color&0x00ff);
	while((USART1->SR&0X40)==0);        //��������
//	if(count < 320) count++;    
//	else
//	{
//		count = 0;
//		printf("\r\n");
//	}
	__asm
	{
		mov  r6,color
		str  r11, [r8]                  // LCD_RS_SET
		str  r12, [r8, 4]               // LCD_CS_CLR
		str  r6, [r1, 0xc]              // GPIOB->ODR = data
		mov  r4, #0x080
		str  r4, [r8, 4]                // LCD_WR_CLR
		str  r0, [r8]                   // LCD_WR_SET, LCD_CS_SET
		sub  r7, r7, 1                  // count down
		cmp  r7, r3
		bne  again;
	}
}

void camera_refresh(void)
{

	if(ov_sta >= 0)
	{
		LCD_Scan_Dir(U2D_L2R);		    //���ϵ���,������
		LCD_SetCursor(0x00,0x0000);	    //���ù��λ��
		LCD_WriteRAM_Prepare();         //��ʼд��GRAM
		OV7670_CS=0;
		OV7670_RRST=0;				    //��ʼ��λ��ָ��
		OV7670_RCK=0;
		OV7670_RCK=1;
		OV7670_RCK=0;
		OV7670_RRST=1;				    //��λ��ָ�����
		OV7670_RCK=1;

		pixels();
		OV7670_CS=1;
		OV7670_RCK=0;
		OV7670_RCK=1;
		EXTI->PR=1<<15;     		    //���LINE8�ϵ��жϱ�־λ
		ov_sta=0;					    //��ʼ��һ�βɼ�
		ov_frame++;
		LCD_Scan_Dir(DFT_SCAN_DIR);	    //�ָ�Ĭ��ɨ�跽��
	}
}
int main(void)
{
	u8 i;

	Stm32_Clock_Init(freq_mul);	        //ϵͳʱ������

	uart_init(72,345600 * 9 / freq_mul);//���ڳ�ʼ��Ϊ9600

	delay_init(72);	   	 	            //��ʱ��ʼ��
	LED_Init();		  		            //��ʼ����LED���ӵ�Ӳ���ӿ�
	LCD_Init();			   	            //��ʼ��LCD
	POINT_COLOR=RED;                    //��������Ϊ��ɫ
	LCD_ShowString(0,0,200,200,16,"OV7670 Init...");
	while(OV7670_Init())                //��ʼ��OV7670
	{
		LCD_ShowString(0,0,200,200,16,"OV7670 Error!!");
		delay_ms(200);
		LCD_Fill(60,150,239,166,WHITE);
		delay_ms(200);
	}
	LCD_ShowString(0,0,200,200,16,"OV7670 Init OK");
	delay_ms(1500);
	TIM3_Int_Init(10000,7199);			//TIM3,10Khz����Ƶ��,1�����ж�
	EXTI15_Init();						//ʹ�ܶ�ʱ������
	OV7670_Window_Set(10,174,240,320);	//���ô���
	OV7670_CS=0;

	while(1)
	{
        while(!CanSend)
        {
            delay_ms(100);
            LCD_ShowString(0,0,200,200,16,"Pleas send 0x00 to me");
        }
//		printf("new pic\r\n");
		camera_refresh();	            //������ʾ
//		printf("end pic\r\n");
        
        CanSend = 0;
        
		if(i!=ov_frame)		            //DS0��˸.
		{
			i=ov_frame;
			LED0=!LED0;
		}
	}
}


