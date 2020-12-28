#include "exti.h"
#include "led.h"
#include "key.h"
#include "delay.h"
#include "usart.h"
#include "ov7670.h"
//////////////////////////////////////////////////////////////////////////////////	 
//本程序只供学习使用，未经作者许可，不得用于其它任何用途
//ALIENTEK Mini STM32开发板
//外部中断 驱动代码			   
//正点原子@ALIENTEK
//技术论坛:www.openedv.com
//修改日期:2014/3/27  
//版本：V1.1
//版权所有，盗版必究。
//Copyright(C) 广州市星翼电子科技有限公司 2009-2019
//All rights reserved				
//********************************************************************************
//V1.1 20140327 
//增加EXTI15_Init函数，并修改EXTI15_10_IRQHandler函数，以支持OV7670的使用
////////////////////////////////////////////////////////////////////////////////// 	  

//外部中断0服务程序
void EXTI0_IRQHandler(void)
{
	delay_ms(10);	//消抖
	if(WK_UP==1)	//WK_UP按键 
	{
		LED0=!LED0;
		LED1=!LED1;	
	}		 
	EXTI->PR=1<<0;  //清除LINE0上的中断标志位  
}
//外部中断9~5服务程序
void EXTI9_5_IRQHandler(void)
{			
	delay_ms(10);   //消抖			 
    if(KEY0==0)		//按键0
	{
		LED0=!LED0;
	}
 	EXTI->PR=1<<5;     //清除LINE5上的中断标志位  
}
////外部中断15~10服务程序
//void EXTI15_10_IRQHandler(void)
//{			
//	delay_ms(10);   //消抖			 
//    if(KEY1==0)		//按键1
//	{
//		LED1=!LED1;
//	}
// 	EXTI->PR=1<<15; //清除LINE15上的中断标志位  
//}
//外部中断初始化程序
//初始化PA0,PC5,PA15为中断输入.
void EXTI_Init(void)
{
	RCC->APB2ENR|=1<<2;     //使能PORTA时钟
	GPIOA->CRL&=0XFFFFFFF0;//PA0设置成输入	  
	GPIOA->CRL|=0X00000008;   
	GPIOA->CRH&=0X0F0FFFFF;//PA13,15设置成输入	  
	GPIOA->CRH|=0X80800000; 				   
	GPIOA->ODR|=1<<13;	   //PA13上拉,PA0默认下拉
	GPIOA->ODR|=1<<15;	   //PA15上拉
	
	Ex_NVIC_Config(GPIO_A,0,RTIR); 		//上升沿触发
	Ex_NVIC_Config(GPIO_C,5,FTIR);		//下降沿触发
	Ex_NVIC_Config(GPIO_A,15,FTIR);		//下降沿触发

	MY_NVIC_Init(2,2,EXTI0_IRQn,2);    	//抢占2，子优先级2，组2
	MY_NVIC_Init(2,1,EXTI9_5_IRQn,2);  	//抢占2，子优先级1，组2
	MY_NVIC_Init(2,0,EXTI15_10_IRQn,2);	//抢占2，子优先级0，组2	   
}
/////////////////////////////////////////////////////////////////////////////////////
//中断服务函数
u8 ov_sta;
void EXTI15_10_IRQHandler(void)
{			
	if(EXTI->PR&(1<<15))//是15线的中断
	{     
		if(ov_sta<2)
		{
			if(ov_sta==0)
			{
				OV7670_WRST=0;	 	//复位写指针		  		 
				OV7670_WRST=1;	
				OV7670_WREN=1;		//允许写入FIFO
			}else OV7670_WREN=0;	//禁止写入FIFO 	 
			ov_sta++;
		}
	}
	EXTI->PR=1<<15;     //清除LINE15上的中断标志位	
}
//外部中断初始化程序
//初始化PA15为中断输入.
void EXTI15_Init(void)
{
	RCC->APB2ENR|=1<<2;     //使能PORTA时钟
	JTAG_Set(SWD_ENABLE);	//关闭JTAG  	 
	GPIOA->CRH&=0X0FFFFFFF;	//PA15设置成输入	  
	GPIOA->CRH|=0X80000000; 				   
 	GPIOA->ODR|=1<<15;	   	//PA15上拉	
	Ex_NVIC_Config(GPIO_A,15,FTIR);//下降沿触发
	MY_NVIC_Init(2,1,EXTI15_10_IRQn,2);//抢占2，子优先级1，组2	   
}














