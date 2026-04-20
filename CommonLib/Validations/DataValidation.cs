using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;   // 정규 표현식 기느을 사용하기 위한 네임 스페이스 

namespace CommonLib.Validations
{
    public class DataValidation
    {
        // 이메일 주소의 유효성을 정규 표현식으로 검사 
        public static bool IsValidEmail(string email)
        {
            if(email == null)
            {
                email = "";
            }

            // Regex.IsMatch: 대상 문자열이 정규식 패턴과 일치하는지 체크합니다.
            // 정규식 패턴 분석:
            // ^ : 문자열의 시작
            // ([\w-\.]+) : 하나 이상의 영문자, 숫자, 언더바(_), 하이픈(-), 마침표(.)가 올 수 있음 (아이디 부분)
            // @ : 반드시 '@' 기호가 포함되어야 함
            // ((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+)) : 
            //    - [192.168.0.1] 처럼 IP 형태의 도메인 혹은
            //    - google.com 처럼 일반적인 문자 형태의 도메인을 허용
            // ([a-zA-Z]{2,4}|[0-9]{1,3}) : 도메인 끝자리 (com, net 등 2~4자 영문 또는 IP 끝자리)
            // (\]?) : IP 주소 형식을 닫는 대괄호(']')가 있을 경우 처리
            // $ : 문자열의 끝

            return Regex.IsMatch(email, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }
    }
}
