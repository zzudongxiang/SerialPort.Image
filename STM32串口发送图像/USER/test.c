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
	while((USART1->SR&0X40)==0);        //发送数据
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
		LCD_Scan_Dir(U2D_L2R);		    //从上到下,从左到右
		LCD_SetCursor(0x00,0x0000);	    //设置光标位置
		LCD_WriteRAM_Prepare();         //开始写入GRAM
		OV7670_CS=0;
		OV7670_RRST=0;				    //开始复位读指针
		OV7670_RCK=0;
		OV7670_RCK=1;
		OV7670_RCK=0;
		OV7670_RRST=1;				    //复位读指针结束
		OV7670_RCK=1;

		pixels();
		OV7670_CS=1;
		OV7670_RCK=0;
		OV7670_RCK=1;
		EXTI->PR=1<<15;     		    //清除LINE8上的中断标志位
		ov_sta=0;					    //开始下一次采集
		ov_frame++;
		LCD_Scan_Dir(DFT_SCAN_DIR);	    //恢复默认扫描方向
	}
}
int main(void)
{
	u8 i;

	Stm32_Clock_Init(freq_mul);	        //系统时钟设置

	uart_init(72,345600 * 9 / freq_mul);//串口初始化为9600

	delay_init(72);	   	 	            //延时初始化
	LED_Init();		  		            //初始化与LED连接的硬件接口
	LCD_Init();			   	            //初始化LCD
	POINT_COLOR=RED;                    //设置字体为红色
	LCD_ShowString(0,0,200,200,16,"OV7670 Init...");
	while(OV7670_Init())                //初始化OV7670
	{
		LCD_ShowString(0,0,200,200,16,"OV7670 Error!!");
		delay_ms(200);
		LCD_Fill(60,150,239,166,WHITE);
		delay_ms(200);
	}
	LCD_ShowString(0,0,200,200,16,"OV7670 Init OK");
	delay_ms(1500);
	TIM3_Int_Init(10000,7199);			//TIM3,10Khz计数频率,1秒钟中断
	EXTI15_Init();						//使能定时器捕获
	OV7670_Window_Set(10,174,240,320);	//设置窗口
	OV7670_CS=0;

	while(1)
	{
        while(!CanSend)
        {
            delay_ms(100);
            LCD_ShowString(0,0,200,200,16,"Pleas send 0x00 to me");
        }
//		printf("new pic\r\n");
		camera_refresh();	            //更新显示
//		printf("end pic\r\n");
        
        CanSend = 0;
        
		if(i!=ov_frame)		            //DS0闪烁.
		{
			i=ov_frame;
			LED0=!LED0;
		}
	}
}


